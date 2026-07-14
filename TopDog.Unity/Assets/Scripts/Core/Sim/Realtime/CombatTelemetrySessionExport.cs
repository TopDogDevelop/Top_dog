using System.Globalization;
using System.Text;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_DIAGNOSTICS.md §会话全量导出
 * 本文件: CombatTelemetrySessionExport.cs — 实时交战全量 telemetry 落盘
 * 【机制要点】
 * · Begin：战役 ChooseRealtime / 约战 / 机制详测 共用 CombatRealtimeLinkService.Begin
 * · 捕获 Begin 前已写入的 combat.fit（spawn 早于握手）
 * · LatestPath 始终覆盖；ArchiveDir 留时间戳副本
 * · 环形缓冲仍 256（UI）；会话缓冲 + 文件为全量
 * 【关联】CombatTelemetryLog · CombatRealtimeLinkService
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>实时交战全量诊断导出（战役 / 约战 / 机制详测共用）。</summary>
public static class CombatTelemetrySessionExport
{
    public const int MaxSessionEntries = 200_000;

    /// <summary>最近一局覆盖写路径（给 Agent / 手测 grep）。</summary>
    public static string LatestPath { get; set; } = @"e:\debug-combat-session.log";

    /// <summary>按局归档目录。</summary>
    public static string ArchiveDir { get; set; } = @"e:\debug-combat";

    public static bool Enabled { get; set; } = true;

    public static bool IsActive { get; private set; }
    public static string? LastExportPath { get; private set; }
    public static string? LastArchivePath { get; private set; }
    public static int SessionLineCount { get; private set; }

    private static readonly object Gate = new();
    private static readonly List<string> SessionEntries = new();
    private static StreamWriter? _writer;
    private static string? _archivePath;
    private static string _sessionId = "";
    private static string _modeLabel = "";

    public static void Begin(GameState state)
    {
        if (!Enabled)
        {
            return;
        }

        // 先拍环形缓冲，避免与 Log→Append 形成锁顺序死锁
        var preexisting = CombatTelemetryLog.SnapshotAll();

        lock (Gate)
        {
            if (IsActive)
            {
                EndUnlocked("superseded");
            }

            _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            _modeLabel = DescribeMode(state);
            SessionEntries.Clear();
            SessionLineCount = 0;
            _archivePath = null;
            IsActive = true;

            try
            {
                Directory.CreateDirectory(ArchiveDir);
                _archivePath = Path.Combine(ArchiveDir, $"session_{_sessionId}.log");
                var dir = Path.GetDirectoryName(LatestPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _writer = new StreamWriter(
                    new FileStream(LatestPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite),
                    Encoding.UTF8)
                {
                    AutoFlush = true,
                };

                WriteLineUnlocked($"--- combat.session.begin id={_sessionId} mode={_modeLabel} ---");
                WriteLineUnlocked(
                    $"meta worldline={state.worldline.type}"
                    + $" campaign={Sanitize(state.campaignName)}"
                    + $" mt={Sanitize(state.mechanismTest?.scenarioId)}"
                    + $" skirmish={(state.skirmish != null)}"
                    + $" activeBf={Sanitize(state.activeBattlefieldId)}"
                    + $" units={CountUnits(state)}");

                // spawn 常在 Begin 之前写完 combat.fit，先并入全量文件
                foreach (var existing in preexisting)
                {
                    WriteLineUnlocked(existing);
                }

                // 机制详测等路径不经 BattlefieldSpawner.LogUnitFit，补一份配装快照
                DumpFitsFromStateUnlocked(state);

                LastExportPath = LatestPath;
                LastArchivePath = _archivePath;
            }
            catch
            {
                _writer?.Dispose();
                _writer = null;
                IsActive = false;
            }
        }
    }

    /// <summary>
    /// 战斗仍在进行但会话被误关（如 UI OnDisable）时重新打开，避免丢齐射/场域/维修日志。
    /// </summary>
    public static void EnsureActive(GameState state)
    {
        if (!Enabled || state == null || !state.combatRealtimeActive)
        {
            return;
        }

        if (IsActive)
        {
            return;
        }

        Begin(state);
        Append("[combat.export] reopened (EnsureActive)");
    }

    public static void Append(string line)
    {
        if (!Enabled || !IsActive || string.IsNullOrEmpty(line))
        {
            return;
        }

        lock (Gate)
        {
            if (!IsActive)
            {
                return;
            }

            WriteLineUnlocked(line);
        }
    }

    /// <summary>仅刷盘，不结束会话（UI 重建时用）。</summary>
    public static void Flush()
    {
        lock (Gate)
        {
            try
            {
                _writer?.Flush();
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>结束会话并写摘要；返回 LatestPath（失败则 null）。</summary>
    public static string? End(string reason = "end")
    {
        if (!Enabled && !IsActive)
        {
            return LastExportPath;
        }

        lock (Gate)
        {
            return EndUnlocked(reason);
        }
    }

    public static string DumpAll()
    {
        lock (Gate)
        {
            return string.Join("\n", SessionEntries);
        }
    }

    public static string StatusLine()
    {
        lock (Gate)
        {
            return $"[combat.export] active={IsActive} lines={SessionLineCount}"
                   + $" latest={LastExportPath ?? "(none)"}"
                   + $" archive={LastArchivePath ?? "(none)"}";
        }
    }

    public static void ResetForTests()
    {
        lock (Gate)
        {
            _writer?.Dispose();
            _writer = null;
            SessionEntries.Clear();
            SessionLineCount = 0;
            IsActive = false;
            _archivePath = null;
            _sessionId = "";
            _modeLabel = "";
        }
    }

    private static string? EndUnlocked(string reason)
    {
        if (!IsActive && _writer == null)
        {
            return LastExportPath;
        }

        try
        {
            WriteSummaryUnlocked(reason);
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;

            if (_archivePath != null && File.Exists(LatestPath))
            {
                File.Copy(LatestPath, _archivePath, overwrite: true);
                LastArchivePath = _archivePath;
            }

            LastExportPath = LatestPath;
        }
        catch
        {
            // ignore IO
        }
        finally
        {
            IsActive = false;
            _writer = null;
        }

        return LastExportPath;
    }

    private static void WriteLineUnlocked(string line)
    {
        if (SessionEntries.Count >= MaxSessionEntries)
        {
            if (SessionEntries.Count == MaxSessionEntries)
            {
                var warn = $"[combat.export] truncated at {MaxSessionEntries} lines";
                SessionEntries.Add(warn);
                try
                {
                    _writer?.WriteLine(warn);
                }
                catch
                {
                    // ignore
                }
            }

            return;
        }

        SessionEntries.Add(line);
        SessionLineCount = SessionEntries.Count;
        try
        {
            _writer?.WriteLine(line);
        }
        catch
        {
            // ignore
        }
    }

    private static void WriteSummaryUnlocked(string reason)
    {
        var salvo = 0;
        var byAttacker = new Dictionary<string, int>(StringComparer.Ordinal);
        var cycles = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var roundsByAttacker = new Dictionary<string, float>(StringComparer.Ordinal);
        var fieldRouteTotal = 0f;
        var fieldRouteByHost = new Dictionary<string, float>(StringComparer.Ordinal);
        var healFloatTotal = 0f;
        var repairRoundTotal = 0f;
        var repairRoundCount = 0;

        foreach (var line in SessionEntries)
        {
            if (line.StartsWith("[combat.salvo]", StringComparison.Ordinal))
            {
                salvo++;
                var body = line.Length > 15 ? line[15..].TrimStart() : line;
                var arrow = body.IndexOf('→');
                if (arrow <= 0)
                {
                    continue;
                }

                var attacker = body[..arrow].Trim();
                byAttacker[attacker] = byAttacker.GetValueOrDefault(attacker) + 1;
                var cycleIdx = body.IndexOf("cycle=", StringComparison.Ordinal);
                if (cycleIdx >= 0)
                {
                    var rest = body[(cycleIdx + 6)..];
                    var end = rest.IndexOf('s');
                    var cycle = end > 0 ? rest[..end] : rest.Split(' ')[0];
                    if (!cycles.TryGetValue(attacker, out var list))
                    {
                        list = new List<string>();
                        cycles[attacker] = list;
                    }

                    if (list.Count < 32)
                    {
                        list.Add(cycle);
                    }
                }

                var roundIdx = body.IndexOf("round=", StringComparison.Ordinal);
                if (roundIdx >= 0)
                {
                    var rest = body[(roundIdx + 6)..];
                    var tok = rest.Split(' ')[0];
                    if (float.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out var rd))
                    {
                        roundsByAttacker[attacker] = roundsByAttacker.GetValueOrDefault(attacker) + rd;
                    }
                }

                continue;
            }

            if (line.StartsWith("[field.route]", StringComparison.Ordinal))
            {
                // [field.route] target→host layer=amount bindOnly=
                var body = line.Length > 13 ? line[13..].TrimStart() : line;
                var arrow = body.IndexOf('→');
                // format: id→host armor=123 bindOnly=
                var parts = body.Split(' ');
                string? host = null;
                if (arrow > 0)
                {
                    var after = body[(arrow + 1)..];
                    host = after.Split(' ')[0].Trim();
                }

                float amount = 0f;
                foreach (var p in parts)
                {
                    if (p.StartsWith("armor=", StringComparison.Ordinal)
                        || p.StartsWith("shield=", StringComparison.Ordinal)
                        || p.StartsWith("struct=", StringComparison.Ordinal))
                    {
                        var v = p[(p.IndexOf('=') + 1)..];
                        float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
                        break;
                    }
                }

                fieldRouteTotal += amount;
                if (!string.IsNullOrEmpty(host))
                {
                    fieldRouteByHost[host!] = fieldRouteByHost.GetValueOrDefault(host!) + amount;
                }

                continue;
            }

            if (line.StartsWith("[combat.float-heal]", StringComparison.Ordinal))
            {
                healFloatTotal += SumSignedAmounts(line);
                continue;
            }

            if (line.StartsWith("[repair.round]", StringComparison.Ordinal))
            {
                repairRoundCount++;
                // healer→target +amount left=
                var plus = line.IndexOf('+');
                if (plus >= 0)
                {
                    var rest = line[(plus + 1)..];
                    var tok = rest.Split(' ')[0];
                    if (float.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out var amt))
                    {
                        repairRoundTotal += amt;
                    }
                }
            }
        }

        WriteLineUnlocked($"--- combat.session.end reason={reason} id={_sessionId} mode={_modeLabel} ---");
        WriteLineUnlocked($"summary lines={SessionLineCount} salvoTotal={salvo}");
        foreach (var kv in byAttacker.OrderByDescending(k => k.Value))
        {
            var cycleList = cycles.TryGetValue(kv.Key, out var c) ? string.Join(",", c) : "-";
            var roundSum = roundsByAttacker.GetValueOrDefault(kv.Key);
            WriteLineUnlocked(
                $"summary.salvo attacker={kv.Key} count={kv.Value} roundSum={roundSum:F0} cycles=[{cycleList}]");
        }

        WriteLineUnlocked($"summary.field-route total={fieldRouteTotal:F0}");
        foreach (var kv in fieldRouteByHost.OrderByDescending(k => k.Value))
        {
            WriteLineUnlocked($"summary.field-route host={kv.Key} absorbed={kv.Value:F0}");
        }

        WriteLineUnlocked(
            $"summary.heal floatHealSum={healFloatTotal:F0} repairRoundCount={repairRoundCount} repairRoundSum={repairRoundTotal:F0}");
    }

    private static float SumSignedAmounts(string line)
    {
        float sum = 0f;
        foreach (var part in line.Split(' '))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = part[..eq];
            if (key is not ("shield" or "armor" or "struct"))
            {
                continue;
            }

            var raw = part[(eq + 1)..].TrimStart('+');
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                sum += Math.Abs(v);
            }
        }

        return sum;
    }

    private static void DumpFitsFromStateUnlocked(GameState state)
    {
        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (u.unitId == null || u.isBuilding)
                {
                    continue;
                }

                var modList = u.fittedModules.Count == 0
                    ? "-"
                    : string.Join(",", u.fittedModules.Select(kv => kv.Key + "=" + kv.Value));
                WriteLineUnlocked(
                    $"[combat.fit] {u.unitId} {Sanitize(u.displayName)} hull={Sanitize(u.hullId)}"
                    + $" mods=[{modList}] salvo={u.salvoRoundDmg:F0} cycle={u.fireCycleSec:F1}s"
                    + $" range={u.attackRangeM / 1000f:F0}km track={u.weaponTrackingDegPerSec:F1}°/s");
            }
        }
    }

    private static string DescribeMode(GameState state)
    {
        if (state.mechanismTest != null && !string.IsNullOrWhiteSpace(state.mechanismTest.scenarioId))
        {
            return "mechanism:" + state.mechanismTest.scenarioId;
        }

        if (state.worldline.type == WorldlineType.LEGION_SKIRMISH || state.skirmish != null)
        {
            return "skirmish";
        }

        return "campaign:" + state.worldline.type;
    }

    private static int CountUnits(GameState state)
    {
        var n = 0;
        foreach (var bf in state.battlefields)
        {
            n += bf.units.Count;
        }

        return n;
    }

    private static string Sanitize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Replace('\n', ' ').Trim();
}
