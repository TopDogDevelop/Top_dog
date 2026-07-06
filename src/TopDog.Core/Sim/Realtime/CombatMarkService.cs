using TopDog.Content.Modules;

/*
 * ══ 设计手册嵌入 ══
 * 权威: 战术场域计划 Phase 3.3 · mod_damage_amp_ray / mod_heal_block_ray
 * 本文件: CombatMarkService.cs — 战斗标记叠乘
 * 【关联】BattlefieldSystem.TryFireSalvo · RemoteRepairSalvoService
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class CombatMarkService
{
    public static void ApplyMarkFromSalvo(
        BattlefieldState bf,
        BattlefieldUnit attacker,
        BattlefieldUnit target,
        ModuleDef mod)
    {
        if (mod.markDurationSec <= 0f)
        {
            return;
        }

        if (mod.incomingDamageMult > 0f && mod.incomingDamageMult != 1f)
        {
            target.combatMarkIncomingMult *= mod.incomingDamageMult;
            target.combatMarkExpireSec = Math.Max(target.combatMarkExpireSec, bf.timeSec + mod.markDurationSec);
        }

        if (mod.outgoingRepairMult > 0f && mod.outgoingRepairMult != 1f)
        {
            target.combatMarkOutgoingRepairMult *= mod.outgoingRepairMult;
            target.combatMarkExpireSec = Math.Max(target.combatMarkExpireSec, bf.timeSec + mod.markDurationSec);
        }
    }

    public static void Tick(BattlefieldState bf)
    {
        foreach (var u in bf.units)
        {
            if (u.combatMarkExpireSec <= 0f)
            {
                continue;
            }

            if (bf.timeSec >= u.combatMarkExpireSec)
            {
                u.combatMarkIncomingMult = 1f;
                u.combatMarkOutgoingRepairMult = 1f;
                u.combatMarkExpireSec = 0f;
            }
        }
    }

    public static float ScaleIncomingDamage(BattlefieldUnit target, float dmg) =>
        dmg * Math.Max(0f, target.combatMarkIncomingMult);

    public static float ScaleOutgoingRepair(BattlefieldUnit target, float repair) =>
        repair * Math.Max(0f, target.combatMarkOutgoingRepairMult);
}
