using TopDog.App.Brick;

namespace TopDog.Sim.Realtime;

/// <summary>战斗开火诊断（BRICK_DEBUG.md · 伤害/射程排障）。</summary>
public static class CombatDamageDiagnostics
{
    public static void LogFire(
        BattlefieldUnit attacker,
        BattlefieldUnit target,
        float distM,
        float dmg)
    {
        if (!BrickDebugLog.Enabled)
        {
            return;
        }

        BrickDebugLog.Log(
            "combat.fire",
            $"{attacker.displayName} → {target.displayName} dist={distM / 1000f:0.1}km"
            + $" range={attacker.attackRangeM / 1000f:0.1}km dps={attacker.damagePerSec:0} hit={dmg:0}");
    }
}
