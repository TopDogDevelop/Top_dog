/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §3
 * 本文件: CombatHostility.cs — 多阵营敌对判定
 * 【机制要点】
 * · combatFactionId 不同 → 敌对；未写则 FRIENDLY=0 / ENEMY=1 回填
 * · 压力关绕过 tonnage 拒锁时仍用本判定选敌
 * 【关联】BattlefieldUnit · AutoFireTargetingService · AoeDamageService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class CombatHostility
{
    public const int PlayerFactionId = 0;

    public static int EffectiveFactionId(BattlefieldUnit u)
    {
        if (u.combatFactionId >= 0)
        {
            return u.combatFactionId;
        }

        return u.side == UnitSide.FRIENDLY ? PlayerFactionId : 1;
    }

    public static bool AreHostile(BattlefieldUnit a, BattlefieldUnit b)
    {
        if (ReferenceEquals(a, b) || a == null || b == null)
        {
            return false;
        }

        return EffectiveFactionId(a) != EffectiveFactionId(b);
    }

    public static UnitSide SideForFaction(int combatFactionId) =>
        combatFactionId == PlayerFactionId ? UnitSide.FRIENDLY : UnitSide.ENEMY;
}
