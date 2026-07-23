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
    /// <summary>模块未写 fieldCollapseCooldownSec 时的回退（秒）；改数值请改模块 JSON。</summary>
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

        TryResumeAfterCollapseCooldown(bf, modules);
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

            if (holder.fieldAuraEnabledAtSec <= 0f || !holder.fieldAuraArmorDominant)
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
        holder.fieldAuraResumeAfterCooldown = false;
        holder.fieldAuraShieldDominant = false;
        holder.fieldAuraArmorDominant = false;
        holder.fieldAuraShieldSuppressed = false;
        holder.fieldAuraArmorSuppressed = false;
    }

    /// <summary>
    /// 崩溃冷却结束后自动续开：仅当场域槽仍在启用限额内（IsSlotEnabled）且对应池 HP&gt;0。
    /// 不调用 SetSlotEnabled(true) 强行挤占配额。
    /// </summary>
    public static void TryResumeAfterCollapseCooldown(BattlefieldState bf, ModuleRegistry modules)
    {
        foreach (var holder in bf.units)
        {
            if (holder.IsDestroyed() || holder.isBuilding || !holder.fieldAuraResumeAfterCooldown)
            {
                continue;
            }

            if (holder.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }

            if (holder.fieldAuraEnabledAtSec > 0f)
            {
                holder.fieldAuraResumeAfterCooldown = false;
                continue;
            }

            var shieldMod = FindFieldModule(holder, modules, "shield_fusion_field");
            var armorMod = FindFieldModule(holder, modules, "armor_link_field");
            var canShield = shieldMod != null && holder.shieldHp > 0f;
            var canArmor = armorMod != null && holder.armorHp > 0f;
            if (!canShield && !canArmor)
            {
                // 槽被限额关掉，或池仍空：保持 resume 标记，等配额/回复
                if (!HasFittedFieldModule(holder, modules))
                {
                    holder.fieldAuraResumeAfterCooldown = false;
                }

                // #region agent log
                try
                {
                    var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                    var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"N\",\"location\":\"FieldAuraService.TryResumeAfterCollapseCooldown\",\"message\":\"resume-wait\",\"data\":{"
                               + "\"holder\":\"" + (holder.unitId ?? "") + "\""
                               + ",\"slotOk\":" + ((shieldMod != null || armorMod != null) ? "true" : "false")
                               + ",\"shieldHp\":" + holder.shieldHp.ToString("F0")
                               + ",\"armorHp\":" + holder.armorHp.ToString("F0")
                               + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                    System.IO.File.AppendAllText(path, line);
                }
                catch
                {
                }
                // #endregion
                continue;
            }

            holder.fieldAuraEnabledAtSec = Math.Max(bf.timeSec, 0.001f);
            holder.fieldAuraResumeAfterCooldown = false;
            // #region agent log
            try
            {
                var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
                var line = "{\"sessionId\":\"85a1e0\",\"runId\":\"post-fix\",\"hypothesisId\":\"N\",\"location\":\"FieldAuraService.TryResumeAfterCollapseCooldown\",\"message\":\"resume-ok\",\"data\":{"
                           + "\"holder\":\"" + (holder.unitId ?? "") + "\""
                           + ",\"canShield\":" + (canShield ? "true" : "false")
                           + ",\"canArmor\":" + (canArmor ? "true" : "false")
                           + ",\"enabledAt\":" + holder.fieldAuraEnabledAtSec.ToString("F1")
                           + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
                System.IO.File.AppendAllText(path, line);
            }
            catch
            {
            }
            // #endregion
        }
    }

    private static bool HasFittedFieldModule(BattlefieldUnit unit, ModuleRegistry modules)
    {
        foreach (var modId in unit.fittedModules.Values)
        {
            var mod = modules.Resolve(modId);
            if (mod != null
                && ("shield_fusion_field".Equals(mod.moduleKind, StringComparison.Ordinal)
                    || "armor_link_field".Equals(mod.moduleKind, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
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

        if ("shield_fusion_field".Equals(mod.moduleKind, StringComparison.Ordinal)
            && hull?.hullShieldFusionRadiusMult > 0f
            && Math.Abs(hull.hullShieldFusionRadiusMult - 1f) > 0.001f)
        {
            km *= hull.hullShieldFusionRadiusMult;
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
            if (u.IsDestroyed() || u.isBuilding || !IsDominantForKind(u, moduleKind))
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
                || !IsDominantForKind(host, moduleKind)
                || DistanceM(protege, host) > radius
                || !EligibleForBinding(protege, host, ships.FindHull(host.hullId), moduleKind))
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
        var holderHull = ships.FindHull(dominant.hullId);
        if (DistanceM(protege, dominant) > dominantRadius
            || !EligibleForBinding(protege, dominant, holderHull, moduleKind))
        {
            return;
        }

        if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            if (EligibleForShieldFusion(protege, dominant, holderHull))
            {
                ApplyEnter(protege, dominant, bf, moduleKind);
                setHostId(dominant.unitId);
                CombatTelemetryLog.LogFieldEnter(protege.unitId!, dominant.unitId!, moduleKind);
            }
            else if (EligibleForBinding(protege, dominant, holderHull, moduleKind))
            {
                setHostId(dominant.unitId);
                CombatTelemetryLog.LogFieldEnter(protege.unitId!, dominant.unitId!, moduleKind + "|bind");
            }
        }
        else
        {
            ApplyEnter(protege, dominant, bf, moduleKind);
            setHostId(dominant.unitId);
            CombatTelemetryLog.LogFieldEnter(protege.unitId!, dominant.unitId!, moduleKind);
        }
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
            if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal))
            {
                protege.shieldFieldHostUnitId = null;
            }
            else if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
            {
                protege.armorFieldHostUnitId = null;
            }

            return;
        }

        if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            if (Bindings.ContainsKey(key))
            {
                holder.shieldMax = Math.Max(0f, holder.shieldMax - binding.shieldContributedCap);
            }

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
                SetDominantForKind(u, moduleKind, false);
                SetSuppressedForKind(u, moduleKind, false);
                continue;
            }

            candidates.Add(u);
        }

        candidates.Sort((a, b) => a.fieldAuraEnabledAtSec.CompareTo(b.fieldAuraEnabledAtSec));
        for (var i = 0; i < candidates.Count; i++)
        {
            var dominant = i == 0;
            SetDominantForKind(candidates[i], moduleKind, dominant);
            SetSuppressedForKind(candidates[i], moduleKind, !dominant);
        }
    }

    public static bool IsDominantForKind(BattlefieldUnit unit, string moduleKind) =>
        "armor_link_field".Equals(moduleKind, StringComparison.Ordinal)
            ? unit.fieldAuraArmorDominant
            : unit.fieldAuraShieldDominant;

    public static void SetDominantForKind(BattlefieldUnit unit, string moduleKind, bool dominant)
    {
        if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            unit.fieldAuraArmorDominant = dominant;
        }
        else
        {
            unit.fieldAuraShieldDominant = dominant;
        }
    }

    private static void SetSuppressedForKind(BattlefieldUnit unit, string moduleKind, bool suppressed)
    {
        if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            unit.fieldAuraArmorSuppressed = suppressed;
        }
        else
        {
            unit.fieldAuraShieldSuppressed = suppressed;
        }
    }

    /// <summary>配装查询（读数值）；不检查启用限额。</summary>
    public static ModuleDef? FindFittedFieldModule(
        BattlefieldUnit unit,
        ModuleRegistry modules,
        string moduleKind)
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

    public static ModuleDef? FindFieldModule(BattlefieldUnit unit, ModuleRegistry modules, string moduleKind)
    {
        foreach (var kv in unit.fittedModules)
        {
            var mod = modules.Resolve(kv.Value);
            if (mod == null || !moduleKind.Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                continue;
            }

            // 启用限额：被 quota/玩家关掉的槽不参与场域
            if (!CombatModuleEnableService.IsSlotEnabled(unit, kv.Key))
            {
                continue;
            }

            return mod;
        }

        return null;
    }

    public static bool IsFieldEligibleProtege(BattlefieldUnit u) =>
        !u.isBuilding
        && !BattlefieldSceneProxyService.IsSceneProxy(u)
        && !u.IsTemplateCarriedUnit()
        && !u.IsBallisticMissile()
        && !"BUILDING".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"COMPLEX".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
        && !"MISSILE".Equals(u.tonnageClass, StringComparison.Ordinal);

    public static int ResolveEffectiveHolderRank(
        BattlefieldUnit holder,
        HullDef? hull,
        string moduleKind)
    {
        if ("shield_fusion_field".Equals(moduleKind, StringComparison.Ordinal)
            && hull?.hullShieldFusionEffectiveTonnageClass != null)
        {
            return CombatPowerCalculator.TonnageRankOf(hull.hullShieldFusionEffectiveTonnageClass);
        }

        return CombatPowerCalculator.TonnageRankOf(holder.tonnageClass);
    }

    public static bool EligibleForBinding(
        BattlefieldUnit protege,
        BattlefieldUnit holder,
        HullDef? holderHull,
        string moduleKind)
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

        if ("armor_link_field".Equals(moduleKind, StringComparison.Ordinal))
        {
            var holderRank = ResolveEffectiveHolderRank(holder, holderHull, moduleKind);
            var protegeRank = CombatPowerCalculator.TonnageRankOf(protege.tonnageClass);
            return protegeRank <= holderRank;
        }

        return true;
    }

    public static bool EligibleForShieldFusion(
        BattlefieldUnit protege,
        BattlefieldUnit holder,
        HullDef? holderHull)
    {
        if (!EligibleForBinding(protege, holder, holderHull, "shield_fusion_field"))
        {
            return false;
        }

        var effectiveRank = ResolveEffectiveHolderRank(holder, holderHull, "shield_fusion_field");
        var holderNativeRank = CombatPowerCalculator.TonnageRankOf(holder.tonnageClass);
        var protegeRank = CombatPowerCalculator.TonnageRankOf(protege.tonnageClass);
        if (protegeRank > holderNativeRank)
        {
            return false;
        }

        return protegeRank <= effectiveRank;
    }

    public static bool Eligible(BattlefieldUnit protege, BattlefieldUnit holder) =>
        EligibleForBinding(protege, holder, null, "shield_fusion_field")
        && EligibleForShieldFusion(protege, holder, null);

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
