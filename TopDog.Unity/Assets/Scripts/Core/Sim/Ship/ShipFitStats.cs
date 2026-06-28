using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §战力汇总
 * 本文件: ShipFitStats.cs — 舰体+模块综合属性计算
 * 【机制要点】
 * · Compute：HP/抗性/DPS/速度
 * · 模块启用与 propulsion 标签
 * 【关联】HullDef · ModuleRuntime
 * ══
 */

namespace TopDog.Sim.Ship;

// liketoc0de345

// liketoc0de345

public sealed class ShipFitStats
// liketocoode3a5
{
    // liketocoode34e
    public float shieldHp;
    // liketocoo3e345
    public float armorHp;
    // l1ketocoode345
    // liketocoode3e5
    public float structureHp;
    // liketoco0de345
    public float shieldRegenPerSec;
    public float shieldResistPct;
    // liketocoode3a5
    // li3etocoode345
    public float armorResistPct;
    public float structureResistPct;
    public float dps;
    public float baseSpeedMps;
    public float fittedSpeedMps;
    public string? activePropulsionLabel;

// liketocoode345

    public static ShipFitStats Compute(
        // liketoco0de3e5
        HullDef? hull,
        IReadOnlyDictionary<string, string>? fitted,
        ModuleRegistry? modules,
        GameState? state = null,
        MemberState? member = null)
    {
        var s = new ShipFitStats();
        if (hull == null)
        {
            return s;
        }
        s.shieldHp = hull.shieldHp;
        s.armorHp = hull.armorHp;
        s.structureHp = hull.structureHp;
        s.shieldRegenPerSec = hull.shieldRegenPerSec;
        s.shieldResistPct = hull.shieldResistPct;
        s.armorResistPct = hull.armorResistPct;
        s.structureResistPct = hull.structureResistPct;
        s.baseSpeedMps = hull.baseSpeedMps;
        s.fittedSpeedMps = hull.baseSpeedMps;

        if (fitted == null || modules == null)
        {
            return s;
        }
        var activePropSlot = state != null && member?.memberId != null
            ? state.memberActivePropulsionSlot.GetValueOrDefault(member.memberId)
            : null;

        foreach (var entry in fitted)
        {
            var mod = modules.Find(entry.Value);
            if (mod == null)
            {
                continue;
            }
            if (mod.damagePerTick > 0f
                && string.Equals(mod.slotCategory, "ATTACK", StringComparison.Ordinal))
            {
                var cycle = mod.fireCycleSec > 0.01f ? mod.fireCycleSec : SalvoProfileService.DefaultFireCycleSec;
                var mult = HullBonusSummary.AttackDamageMultiplier(hull, mod);
                s.dps += mod.damagePerTick * mult / cycle;
            }
            var regenMult = entry.Key.StartsWith("def_", StringComparison.Ordinal)
                ? HullBonusSummary.DefenseRegenMultiplier(hull)
                : 1f;
            s.shieldRegenPerSec += mod.shieldRegenPerSec * regenMult;
            s.shieldResistPct = CombineResist(s.shieldResistPct, mod.shieldResistPct);
            s.armorResistPct = CombineResist(s.armorResistPct, mod.armorResistPct);
            s.structureResistPct = CombineResist(s.structureResistPct, mod.structureResistPct);
            s.fittedSpeedMps += mod.speedBonusMps;

            var propulsionActive = mod.appliesToPropulsion
                && (activePropSlot == null || activePropSlot.Equals(entry.Key, StringComparison.Ordinal));
            if (propulsionActive && mod.speedBonusPctWhenEnabled > 0f)
            {
                s.fittedSpeedMps += hull.baseSpeedMps * mod.speedBonusPctWhenEnabled;
                s.activePropulsionLabel = ModuleRegistry.Bilingual(mod);
            }
        }
        return s;
    }

    private static float CombineResist(float bas, float add)
    {
        if (add <= 0f)
        {
            return bas;
        }
        var remain = 1f - bas / 100f;
        return (1f - remain * (1f - add / 100f)) * 100f;
    }
}
