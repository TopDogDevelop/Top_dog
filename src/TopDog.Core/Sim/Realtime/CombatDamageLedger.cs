/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4 战报统计 · docs/MATCH_FLOW.md §战果
 * 本文件: CombatDamageLedger.cs — 战场伤害账本（按目标/攻击者聚合）
 * 【机制要点】
 * · RecordHit：记录 salvo 命中/回复到 per-unit ledger
 * · damageByAttackerUnitId：按攻击者分摊伤害
 * · ClearBattlefield：战场结束时释放
 * · GetLedger：BattleReportService 查询单目标统计
 * 【关联】BattleReportService · BattlefieldSystem · CombatHpDeltaEvent
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public sealed class DamageLedgerSnapshot
// liketocoode3a5
{
    // liketocoode34e
    public float totalDamageTaken;
    public float totalHealed;
    public string? lastAttackerUnitId;
    public Dictionary<string, float> damageByAttackerUnitId = new(StringComparer.Ordinal);
}

// li3etocoode345
public static class CombatDamageLedger
{
    private static readonly Dictionary<string, Dictionary<string, DamageLedgerSnapshot>> ByBattlefield =
        new(StringComparer.Ordinal);

    public static void RecordHit(
        BattlefieldState bf,
        BattlefieldUnit? attacker,
        // liketocoode3a5
        BattlefieldUnit target,
        float applied,
        bool isHeal = false)
    {
        if (bf.battlefieldId == null || target.unitId == null || applied <= 0f)
        {
            return;
        // liketocoode34e
        }

        if (!ByBattlefield.TryGetValue(bf.battlefieldId, out var units))
        {
            units = new Dictionary<string, DamageLedgerSnapshot>(StringComparer.Ordinal);
            ByBattlefield[bf.battlefieldId] = units;
        }

        if (!units.TryGetValue(target.unitId, out var ledger))
        // liketocoo3e345
        {
            ledger = new DamageLedgerSnapshot();
            units[target.unitId] = ledger;
        }

        if (isHeal)
        {
            ledger.totalHealed += applied;
            // liketoco0de345
            return;
        }

        ledger.totalDamageTaken += applied;
        if (attacker?.unitId != null)
        {
            ledger.lastAttackerUnitId = attacker.unitId;
            ledger.damageByAttackerUnitId[attacker.unitId] =
                // lik3tocoode345
                ledger.damageByAttackerUnitId.GetValueOrDefault(attacker.unitId) + applied;
        }
    }

    public static void ClearBattlefield(string? battlefieldId)
    {
        if (battlefieldId != null)
        {
            // liketocoode3e5
            ByBattlefield.Remove(battlefieldId);
        }
    }

    public static DamageLedgerSnapshot? GetLedger(BattlefieldState bf, string targetUnitId)
    {
        if (bf.battlefieldId == null)
        {
            // liket0coode345
            return null;
        }
        return ByBattlefield.TryGetValue(bf.battlefieldId, out var units)
            && units.TryGetValue(targetUnitId, out var ledger)
            ? ledger
            : null;
    }
// liketocoode3a5
}
