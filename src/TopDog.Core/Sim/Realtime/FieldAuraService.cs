using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §1–§5 · §4b
 * 本文件: FieldAuraService.cs — 场域 1Hz 进出与主导场
 * 【机制要点】
 * · Eligible：持有舰不可自纳入甲/盾场（protege.unitId != holder.unitId）
 * · 被连接舰无数量上限；甲池 = 自身 + 场内已连接舰贡献
 * · ApplyGreyWolfShieldBonus：线性 +50%/只短腿狼；shieldHp 随上限同步增加
 * · 遥测 field.enter / field.leave（CombatTelemetryLog）
 * 【关联】FieldAuraCollapse · FieldAuraDamageRouter · FieldAuraWarpGate
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class FieldAuraService
{
    public const float TickIntervalSec = 1f;
    public const float FieldCollapseCooldownSec = 30f;

    private static readonly Dictionary<string, float> LastTickByBf = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ProtegeBinding> Bindings = new(StringComparer.Ordinal);

    private sealed class ProtegeBinding
    {
        public float shieldContributedCap;
        public float shieldContributedCurrent;
        public float entryArmorCap;
        public float entryArmorCurrent;
    }

    public static void Tick(
        GameState state,
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships,
        float dtSec)
    {
        if (bf.battlefieldId == null)
        {
            return;
        }

        if (!ShouldRunHzTick(bf, dtSec))
        {
            return;
        }

        RefreshDominantField(bf, modules, "shield_fusion_field");
        RefreshDominantField(bf, modules, "armor_link_field");

        foreach (var holder in bf.units)
        {
            if (holder.IsDestroyed() || holder.isBuilding)
            {
                continue;
            }

            var shieldMod = FindFieldModule(holder, modules, "shield_fusion_field");
            var armorMod = FindFieldModule(holder, modules, "armor_link_field");
            if ((shieldMod != null || armorMod != null) && holder.fieldAuraEnabledAtSec <= 0f)
            {
                continue;
            }

            if (shieldMod == null && armorMod == null)
            {
                continue;
            }

            if (holder.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }
        }

        foreach (var protege in bf.units)
        {
            if (protege.IsDestroyed() || protege.isBuilding || !IsFieldEligibleProtege(protege))
            {
                continue;
            }

            TickProtegeKind(bf, protege, modules, ships, "shield_fusion_field", protege.shieldFieldHostUnitId,
                id => protege.shieldFieldHostUnitId = id);
            TickProtegeKind(bf, protege, modules, ships, "armor_link_field", protege.armorFieldHostUnitId,
                id => protege.armorFieldHostUnitId = id);
        }

        TickGreyWolfArmorFieldBonuses(bf, modules, ships);
    }

    private static void TickGreyWolfArmorFieldBonuses(
        BattlefieldState bf,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        foreach (var holder in bf.units)
        {
            if (holder.IsDestroyed() || holder.isBuilding)
            {
                continue;
            }

            if (FindFieldModule(holder, modules, "armor_link_field") == null)
            {
                continue;
            }

            if (holder.fieldAuraEnabledAtSec <= 0f || !holder.fieldAuraDominant)
            {
                continue;
            }

            if (holder.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }

            ApplyGreyWolfShieldBonus(holder, bf, ships.FindHull(holder.hullId));
        }
    }

    public static void DisableFieldAndSettleAll(BattlefieldUnit holder, BattlefieldState bf, ModuleRegistry modules)
    {
        SettleAllProteges(holder, bf, "shield_fusion_field", collapse: false);
        SettleAllProteges(holder, bf, "armor_link_field", collapse: false);
        holder.fieldAuraEnabledAtSec = 0f;
        holder.fieldAuraDominant = false;
        holder.fieldAuraSuppressed = false;
    }

    public static float ResolveFieldRadiusM(BattlefieldUnit holder, ModuleDef mod, HullDef? hull)
    {
        var km = mod.fieldRadiusKm > 0f
            ? mod.fieldRadiusKm
            : mod.moduleSize?.ToUpperInvariant() switch
            {
                "SMALL" or "S" => 5f,
                "MEDIUM" or "M" => 10f,
                "LARGE" or "L" => 15f,
                _ => 10f,
            };

        if ("armor_link_field".Equals(mod.moduleKind, StringComparison.Ordinal)
            && hull?.hullArmorLinkSmallRadiusBonusKm > 0f
            && mod.moduleId != null
            && mod.moduleId.Contains("armor_link_s", StringComparison.Ordinal))
        {
            km += hull.hullArmorLinkSmallRadiusBonusKm;
        }

        return km * 1000f;
    }

    public static BattlefieldUnit? FindDominantHolder(
        BattlefieldState bf,
        ModuleRegistry modules,
        string moduleKind)
    {
        BattlefieldUnit? best = null;
        var bestAt = float.MaxValue;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || !u.fieldAuraDominant)
            {
                continue;
            }

            var mod = FindFieldModule(u, modules, moduleKind);
            if (mod == null || u.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }

            if (u.fieldAuraEnabledAtSec < bestAt)
            {
                bestAt = u.fieldAuraEnabledAtSec;
                best = u;
            }
        }

        return best;
    }

    public static void ApplyGreyWolfShieldBonus(BattlefieldUnit holder, BattlefieldState bf, HullDef? hull)
    {
        if (hull?.hullFieldProtegeShieldBonusPct <= 0f
            || holder.unitId == null
            || !"hull_cruiser_greywolf_guard".Equals(holder.hullId, StringComparison.Ordinal))
        {
            return;
        }

        var count = 0;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed()
                || !"hull_frigate_shortlegwolf".Equals(u.hullId, StringComparison.Ordinal))
            {
                continue;
            }

            if (holder.unitId.Equals(u.armorFieldHostUnitId, StringComparison.Ordinal))
            {
                count++;
            }
        }

        var bonusPct = hull.hullFieldProtegeShieldBonusPct * count;
        var baseShield = hull.shieldHp;
        var oldMax = holder.shieldMax;
        var newMax = baseShield * (1f + bonusPct / 100f);
        holder.shieldMax = newMax;
        if (newMax > oldMax)
        {
            holder.shieldHp += newMax - oldMax;
        }
        else if (holder.shieldHp > holder.shieldMax)
        {
            // §2.3b: allow over-cap
        }
        else
        {
            holder.shieldHp = Math.Min(holder.shieldHp, holder.shieldMax);
        }
    }

    private static void TickProtegeKind(
        BattlefieldState bf,
        BattlefieldUnit protege,
        ModuleRegistry modules,
        ShipRegistry ships,
        string moduleKind,
        string? hostId,
        Action<string?> setHostId)
    {
        BattlefieldUnit? host = null;
        if (hostId != null)
        {
            host = BattlefieldSystem.FindUnit(bf, hostId);
            var mod = host != null ? FindFieldModule(host, modules, moduleKind) : null;
            var radius = host != null && mod != null
                ? ResolveFieldRadiusM(host, mod, ships.FindHull(host.hullId))
                : 0f;

            if (host == null
                || host.IsDestroyed()
                || mod == null
                || host.fieldAuraCollapseCooldownSec > bf.timeSec
                || !host.fieldAuraDominant
                || DistanceM(protege, host) > radius
                || !Eligible(protege, host))
            {
                if (host != null)
                {
                    ApplyLeave(protege, host, bf, moduleKind, collapse: false);
                }

                setHostId(null);
                host = null;
                if (protege.unitId != null && hostId != null)
                {
                    CombatTelemetryLog.LogFieldLeave(protege.unitId, hostId, moduleKind);
                }
            }
        }

        if (host != null)
        {
            return;
        }

        var dominant = FindDominantHolder(bf, modules, moduleKind);
        if (dominant == null || dominant.unitId == null)
        {
            return;
        }

        var dominantMod = FindFieldModule(dominant, modules, moduleKind);
        if (dominantMod == null)
        {
            return;
        }

        var dominantRadius = ResolveFieldRadiusM(
            dominant, dominantMod, ships.FindHull(dominant.hullId));
        if (DistanceM(protege, dominant) > dominantRadius || !Eligible(protege, dominant))
        {
            return;
        }

        ApplyEnter(protege, dominant, bf, moduleKind);
        setHostId(dominant.unitId);
        CombatTelemetryLog.LogFieldEnter(protege.unitId!, dominant.unitId!, moduleKind);
    }

    public static void ApplyEnter(
        BattlefieldUnit protege,
        BattlefieldUnit holder,
        BattlefieldState bf,
        string moduleKind)
    {
        var key = BindingKey(protege.unitId!, moduleKind);
        var binding = Bindings.GetValueOrDefault(key) ?? new ProtegeBinding();

        if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            binding.shieldContributedCap = protege.shieldMax;
            binding.shieldContributedCurrent = protege.shieldHp;
            protege.shieldHp = 0f;
            holder.shieldMax += binding.shieldContributedCap;
            holder.shieldHp += binding.shieldContributedCurrent;
        }
        else if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            binding.entryArmorCap = protege.armorMax;
            binding.entryArmorCurrent = protege.armorHp;
            protege.fieldEntryArmorCap = binding.entryArmorCap;
            protege.fieldEntryArmorCurrent = binding.entryArmorCurrent;
            protege.armorHp = 0f;
            holder.armorMax += binding.entryArmorCap;
            holder.armorHp += binding.entryArmorCurrent;
            if (holder.armorHp > holder.armorMax)
            {
                holder.armorHp = holder.armorMax;
            }
        }

        Bindings[key] = binding;
    }

    public static void ApplyLeave(
        BattlefieldUnit protege,
        BattlefieldUnit holder,
        BattlefieldState bf,
        string moduleKind,
        bool collapse)
    {
        if (protege.unitId == null)
        {
            return;
        }

        var key = BindingKey(protege.unitId, moduleKind);
        if (!Bindings.TryGetValue(key, out var binding))
        {
            return;
        }

        if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            holder.shieldMax = Math.Max(0f, holder.shieldMax - binding.shieldContributedCap);
            protege.shieldHp = 0f;
            protege.shieldFieldHostUnitId = null;
        }
        else if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            holder.armorMax = Math.Max(0f, holder.armorMax - binding.entryArmorCap);
            holder.armorHp = Math.Max(0f, holder.armorHp - binding.entryArmorCurrent);
            if (collapse)
            {
                protege.armorHp = 0f;
            }
            else
            {
                protege.armorMax = binding.entryArmorCap;
                protege.armorHp = binding.entryArmorCurrent;
            }

            protege.armorFieldHostUnitId = null;
        }

        Bindings.Remove(key);
    }

    public static void SettleAllProteges(BattlefieldUnit holder, BattlefieldState bf, string moduleKind, bool collapse)
    {
        foreach (var protege in bf.units)
        {
            if (protege.unitId == null || protege.IsDestroyed())
            {
                continue;
            }

            var isBound = "shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal)
                ? holder.unitId != null
                  && holder.unitId.Equals(protege.shieldFieldHostUnitId, StringComparison.Ordinal)
                : holder.unitId != null
                  && holder.unitId.Equals(protege.armorFieldHostUnitId, StringComparison.Ordinal);

            if (!isBound)
            {
                continue;
            }

            ApplyLeave(protege, holder, bf, moduleKind, collapse);
        }
    }

    public static void RefreshDominantField(BattlefieldState bf, ModuleRegistry modules, string moduleKind)
    {
        var candidates = new List<BattlefieldUnit>();
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding)
            {
                continue;
            }

            var mod = FindFieldModule(u, modules, moduleKind);
            if (mod == null || u.fieldAuraCollapseCooldownSec > bf.timeSec || u.fieldAuraEnabledAtSec <= 0f)
            {
                u.fieldAuraDominant = false;
                u.fieldAuraSuppressed = false;
                continue;
            }

            candidates.Add(u);
        }

        candidates.Sort((a, b) => a.fieldAuraEnabledAtSec.CompareTo(b.fieldAuraEnabledAtSec));
        for (var i = 0; i < candidates.Count; i++)
        {
            var dominant = i == 0;
            candidates[i].fieldAuraDominant = dominant;
            candidates[i].fieldAuraSuppressed = !dominant;
        }
    }

    public static ModuleDef? FindFieldModule(BattlefieldUnit unit, ModuleRegistry modules, string moduleKind)
    {
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod != null && moduleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                return mod;
            }
        }

        return null;
    }

    public static bool IsFieldEligibleProtege(BattlefieldUnit u) =>
        !u.isBuilding
        && !BattlefieldSceneProxyService.IsSceneProxy(u)
        && u.parentUnitId == null
        && !u.IsBallisticMissile()
        && !"BUILDING".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"COMPLEX".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"MISSILE".Equals(u.tonnageClass, StringComparison.Ordinal);

    public static bool Eligible(BattlefieldUnit protege, BattlefieldUnit holder)
    {
        if (protege.unitId != null
            && holder.unitId != null
            && protege.unitId.Equals(holder.unitId, StringComparison.Ordinal))
        {
            return false;
        }

        if (protege.side != holder.side)
        {
            return false;
        }

        if (!IsFieldEligibleProtege(protege))
        {
            return false;
        }

        var holderRank = CombatPowerCalculator.TonnageRankOf(holder.tonnageClass);
        var protegeRank = CombatPowerCalculator.TonnageRankOf(protege.tonnageClass);
        return protegeRank <= holderRank;
    }

    public static float DistanceM(BattlefieldUnit a, BattlefieldUnit b)
    {
        var dx = a.x - b.x;
        var dy = a.y - b.y;
        var dz = a.z - b.z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static bool ShouldRunHzTick(BattlefieldState bf, float dtSec)
    {
        var id = bf.battlefieldId!;
        var acc = LastTickByBf.GetValueOrDefault(id);
        acc += dtSec;
        if (acc < TickIntervalSec)
        {
            LastTickByBf[id] = acc;
            return false;
        }

        LastTickByBf[id] = acc - TickIntervalSec;
        return true;
    }

    private static string BindingKey(string protegeId, string moduleKind) =>
        protegeId + "|" + moduleKind;
}
