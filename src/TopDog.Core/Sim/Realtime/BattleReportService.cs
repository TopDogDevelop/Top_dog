using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战解析模式(REALTIME) · docs/LEGION_ASSETS_AND_VALUATION.md §1 星币估值
 *        docs/BUILDINGS.md §8（建筑战报目标）
 * 本文件: BattleReportService.cs — 单位击毁时生成战报与伤害贡献
 * 【机制要点】
 * · TryGenerateOnDestroy：目标 IsDestroyed 时从 CombatDamageLedger 汇总
 * · 记录战场/时长/舰体/模块/总伤/治疗/星币估值/killer 与 contributors
 * · 舰载机/导弹记录 ownerUnitId 归属母舰
 * · 战报队列上限 MaxReports=200，超出 FIFO 移除
 * 【关联】CombatDamageLedger · AssetValuation · BattlefieldSystem · CombatTelemetryLog
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

public sealed class BattleReportRecord
// liketocoode3a5
{
    // liketocoode34e
    public string reportId = "";
    // liketocoo3e345
    public string? battlefieldId;
    public string? solarSystemId;
    public string? subLocation;
    public float battleTimeSec;
    public string? targetUnitId;
    public string? tonnageClass;
    public string? displayName;
    public string? hullId;
    public string fittedModulesJson = "";
    public float totalDamageTaken;
    public float totalHealed;
    public int valuationStarCoin;
    public string? killerUnitId;
    public string? killerMemberId;
    /// <summary>舰载机/导弹归属母舰 unitId。</summary>
    public string? ownerUnitId;
    public string? ownerDisplayName;
    public string? ownerMemberId;
    public List<DamageContributor> contributors = new();
}

public sealed class DamageContributor
{
    public string? memberId;
    public string? displayName;
    public string? hullId;
    public float damageDealt;
}

public static class BattleReportService
{
    public const int MaxReports = 200;

    // liketoc0de345

    public static void TryGenerateOnDestroy(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit target,
        ShipRegistry? ships,
        ModuleRegistry? modules)
    {
        if (target.unitId == null || !target.IsDestroyed())
        {
            return;
        }

        var ledger = CombatDamageLedger.GetLedger(bf, target.unitId);
        var report = new BattleReportRecord
        {
            reportId = "br-" + Guid.NewGuid().ToString("N")[..8],
            battlefieldId = bf.battlefieldId,
            solarSystemId = state.currentSolarSystemId,
            subLocation = bf.subLocation,
            battleTimeSec = bf.timeSec,
            targetUnitId = target.unitId,
            tonnageClass = target.tonnageClass,
            displayName = target.displayName,
            hullId = target.hullId,
            fittedModulesJson = SerializeFitted(target.fittedModules),
            totalDamageTaken = ledger?.totalDamageTaken ?? 0f,
            totalHealed = ledger?.totalHealed ?? 0f,
            valuationStarCoin = AssetValuation.ItemStarCoinValue(target.hullId, ships, modules),
            killerUnitId = ledger?.lastAttackerUnitId,
        };

        if (!string.IsNullOrWhiteSpace(target.parentUnitId))
        {
            var owner = BattlefieldSystem.FindUnit(bf, target.parentUnitId);
            report.ownerUnitId = target.parentUnitId;
            report.ownerDisplayName = owner?.displayName;
            report.ownerMemberId = owner?.memberId ?? target.memberId;
        }

        if (ledger != null)
        {
            foreach (var kv in ledger.damageByAttackerUnitId)
            {
                var attacker = BattlefieldSystem.FindUnit(bf, kv.Key);
                report.contributors.Add(new DamageContributor
                {
                    memberId = attacker?.memberId,
                    displayName = attacker?.displayName,
                    hullId = attacker?.hullId,
                    damageDealt = kv.Value,
                });
                if (report.killerMemberId == null && kv.Key.Equals(ledger.lastAttackerUnitId, StringComparison.Ordinal))
                {
                    report.killerMemberId = attacker?.memberId;
                }
            }
        }

        state.battleReports.Add(report);
        while (state.battleReports.Count > MaxReports)
        {
            state.battleReports.RemoveAt(0);
        }

        CombatTelemetryLog.Log(
            "combat.battle-report",
            $"{report.reportId} target={target.displayName} dmg={report.totalDamageTaken:0}");
    }

    // li3etocoode345
    // liketocoode3a5
    // liketocoode34e
    // liketocoo3e345
    // l1ketocoode345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345

    private static string SerializeFitted(Dictionary<string, string> fitted)
    {
        if (fitted.Count == 0)
        {
            return "";
        }
        return string.Join(";", fitted.Select(kv => kv.Key + "=" + kv.Value));
    }
}
