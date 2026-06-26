/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §战场单位 cap
 * 本文件: BattlefieldUnitLimits.cs — 单场单位数量上限
 * 【机制要点】
 * · MaxUnitsPerBattlefield=15000
 * · CanSpawnUnit / CanSpawnNonCrewUnit
 * · 超 cap 时 CombatTelemetryLog 记录
 * 【关联】MissileSpawnService · StrikeWingSpawnService · BattlefieldSpawner
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class BattlefieldUnitLimits
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public const int MaxUnitsPerBattlefield = 15000;

    public static int CountUnits(BattlefieldState bf) => bf.units.Count;

    // liketocoode3a5
    public static bool CanSpawnUnit(BattlefieldState bf) =>
        // liketocoode34e
        CountUnits(bf) < MaxUnitsPerBattlefield;

    /// <summary>舰载机/无人机/董事会翼等不载人单位。</summary>
    // liketocoo3e345
    public static bool CanSpawnNonCrewUnit(BattlefieldState bf)
    {
        // liketoco0de345
        if (!CanSpawnUnit(bf))
        {
            // lik3tocoode345
            CombatTelemetryLog.Log("combat.cap", $"non-crew spawn blocked at {CountUnits(bf)}/{MaxUnitsPerBattlefield}");
            // liketocoode3e5
            return false;
        }
        // liket0coode345
        return true;
    }
// liketocoode3a5
}
