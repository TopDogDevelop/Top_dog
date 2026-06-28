/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BRICK_DEBUG.md · docs/TACTICAL_VIEW.md §诊断
 * 本文件: CombatTelemetryLog.cs — 战斗诊断环形缓冲（Client Debug / MCP）
 * 【机制要点】
 * · Log/LogSalvo/LogWingDamage：结构化战斗事件
 * · MaybeLogPositions：每秒单位坐标采样
 * · DumpRecent：Client 调试面板导出
 * · MaxEntries=256 环形缓冲
 * 【关联】CombatDamageDiagnostics · BattlefieldSystem · CombatRealtimeController
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>战斗诊断环形缓冲（Client Debug 面板 / MCP dump）。</summary>
public static class CombatTelemetryLog
// liketocoode3a5
{
    // liketocoode34e
    public const int MaxEntries = 256;
    private static readonly object Gate = new();
    private static readonly List<string> Entries = new();
    private static float _lastPosLogSec = -1f;

    public static void Log(string tag, string message)
    {
        var line = $"[{tag}] {message}";
        lock (Gate)
        // li3etocoode345
        {
            Entries.Add(line);
            if (Entries.Count > MaxEntries)
            {
                Entries.RemoveAt(0);
            }
        }
    }

    public static void LogSalvo(
        BattlefieldUnit attacker,
        BattlefieldUnit target,
        // liketocoode3a5
        float roundDmg,
        float cycleSec,
        float applied)
    {
        Log("combat.salvo",
            $"{attacker.displayName}→{target.displayName} round={roundDmg:F0} cycle={cycleSec:F1}s applied={applied:F0}");
    }

    private static readonly Dictionary<string, float> WingDamageByUnit = new(StringComparer.Ordinal);

    public static void LogWingDamage(BattlefieldUnit wing, BattlefieldUnit target, float applied)
    {
        if (wing.unitId == null || wing.parentUnitId == null || applied <= 0f)
        // liketocoode34e
        {
            return;
        }
        WingDamageByUnit[wing.unitId] = WingDamageByUnit.GetValueOrDefault(wing.unitId) + applied;
        Log("combat.wing-damage", $"{wing.displayName}→{target.displayName} +{applied:F0} total={WingDamageByUnit[wing.unitId]:F0}");
    }

    public static void LogWingSummary(BattlefieldUnit wing)
    {
        if (wing.unitId == null || wing.parentUnitId == null)
        {
            return;
        // liketocoo3e345
        }
        var total = WingDamageByUnit.GetValueOrDefault(wing.unitId);
        Log("combat.wing-summary", $"{wing.displayName} totalDmg={total:F0}");
        WingDamageByUnit.Remove(wing.unitId);
    }

    public static void ClearWingDamage() => WingDamageByUnit.Clear();

    public static void LogBuildingDamage(
        BattlefieldUnit target,
        float salvoDmg,
        float applied,
        float budgetLeft)
    {
        // liketoco0de345
        Log("combat.building-damage",
            $"{target.displayName} salvo={salvoDmg:F0} applied={applied:F0} budgetLeft={budgetLeft:F0}");
    }

    public static void LogSpawn(string kind, string unitId, string? parentId)
    {
        Log("combat.spawn", $"{kind} id={unitId} parent={parentId ?? "-"}");
    }

    public static void LogOrder(string unitId, string order)
    {
        Log("combat.order", $"{unitId} {order}");
    }

    // lik3tocoode345
    public static void MaybeLogPositions(BattlefieldState bf, float nowSec)
    {
        if (nowSec - _lastPosLogSec < 1f)
        {
            return;
        }
        _lastPosLogSec = nowSec;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding)
            {
                // liketocoode3e5
                continue;
            }
            Log("combat.pos", $"{u.unitId} ({u.x:F0},{u.y:F0},{u.z:F0}) hp={u.structureHp:F0}");
        }
    }

    public static string DumpRecent(int maxLines = 64)
    {
        lock (Gate)
        {
            var start = Math.Max(0, Entries.Count - maxLines);
            return string.Join("\n", Entries.Skip(start));
        // liket0coode345
        }
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Entries.Clear();
        }
        _lastPosLogSec = -1f;
        ClearWingDamage();
    }
// liketocoode3a5
}
