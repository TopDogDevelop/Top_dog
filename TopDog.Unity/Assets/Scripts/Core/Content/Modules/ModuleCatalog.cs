/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/SHIPS.md §模块 · CONTENT_FORMAT.md
 * 本文件: ModuleCatalog.cs — 可装备物品 ID 判定
 * 【机制要点】
 * · mod_/plug_ 前缀
 * · strike_wing / chaos_missile 等特例
 * 【关联】ModuleRegistry · FittingValidator
 * ══
 */

namespace TopDog.Content.Modules;

// liketoc0de345

// liketoc0de345

public static class ModuleCatalog
// liketocoode3a5
{
    /// <summary>Inventory ids that may be equipped (JSON, mod_/plug_ prefix, or legacy wing/missile ids).</summary>
    // liketocoode34e
    public static bool IsEquippableInventoryId(string? itemId)
    // liketocoo3e345
    {
        if (string.IsNullOrWhiteSpace(itemId))
        // liketocoode3a5
        {
            return false;
        // l1ketocoode345
        }
        if (itemId.StartsWith("mod_", StringComparison.Ordinal)
            || itemId.StartsWith("plug_", StringComparison.Ordinal))
        {
            // liketocoode3e5
            return true;
        }
        if (itemId.StartsWith("res_", StringComparison.Ordinal)
            || itemId.StartsWith("hull_", StringComparison.Ordinal))
        {
            return false;
        // liketoco0de345
        }
        return itemId.Contains("strike_wing", StringComparison.Ordinal)
               || itemId.Contains("chaos_missile", StringComparison.Ordinal)
               || (itemId.Contains("missile", StringComparison.Ordinal) && !itemId.Contains("gun", StringComparison.Ordinal));
    }

// li3etocoode345

    // liketocoode345
    public static bool IsMissileModuleId(string? modId) =>
        // liketoco0de3e5
        !string.IsNullOrWhiteSpace(modId)
        && !modId.Contains("strike_wing", StringComparison.Ordinal)
        && (modId.Contains("chaos_missile", StringComparison.Ordinal)
            || (modId.Contains("missile", StringComparison.Ordinal) && !modId.Contains("gun", StringComparison.Ordinal)));

    public static ModuleDef? Resolve(ModuleRegistry registry, string? moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return null;
        }
        var known = registry.Find(moduleId);
        if (known != null)
        {
            return known;
        }
        return Stub(moduleId);
    }

    public static ModuleDef Stub(string moduleId)
    {
        var m = new ModuleDef
        {
            moduleId = moduleId,
            displayName = DisplayNameZh(moduleId),
            displayNameEn = DisplayNameEn(moduleId),
            slotCategory = InferCategory(moduleId),
            moduleSize = InferSize(moduleId),
        };
        if (moduleId.StartsWith("plug_", StringComparison.Ordinal))
        {
            m.moduleKind = "stat_plugin";
        }
        if (moduleId.Contains("ore_mining", StringComparison.Ordinal))
        {
            m.moduleKind = "mining_beam";
            m.miningYieldPerOpsPhase = 500f;
            m.miningResourceId = ResourceIds.Inorganic;
        }
        ApplyStubStats(m);
        return m;
    }

    private static string InferCategory(string id)
    {
        if (id.StartsWith("plug_", StringComparison.Ordinal))
        {
            return "PASSIVE";
        }
        if (id.Contains("ore_mining", StringComparison.Ordinal) || id.Contains("mining_beam", StringComparison.Ordinal))
        {
            return "ATTACK";
        }
        if (id.Contains("strike_wing", StringComparison.Ordinal) || id.Contains("chaos_missile", StringComparison.Ordinal)
            || (id.Contains("missile", StringComparison.Ordinal) && !id.Contains("gun", StringComparison.Ordinal)))
        {
            return "LAUNCH_TUBE";
        }
        if (id.Contains("hybrid_gun", StringComparison.Ordinal) || id.Contains("_gun_", StringComparison.Ordinal)
            || id.EndsWith("_gun", StringComparison.Ordinal) || id.Contains("artillery", StringComparison.Ordinal)
            || id.Contains("turret", StringComparison.Ordinal))
        {
            return "ATTACK";
        }
        if (id.Contains("shield_regen", StringComparison.Ordinal) || id.Contains("armor_regen", StringComparison.Ordinal)
            || id.Contains("shield_resist", StringComparison.Ordinal) || id.Contains("armor_resist", StringComparison.Ordinal))
        {
            return "DEFENSE";
        }
        if (id.Contains("scanner", StringComparison.Ordinal) || id.Contains("warp_scram", StringComparison.Ordinal)
            || id.Contains("web", StringComparison.Ordinal) || id.Contains("disrupt", StringComparison.Ordinal)
            || id.Contains("drain", StringComparison.Ordinal) || id.Contains("propulsion", StringComparison.Ordinal)
            || id.Contains("damage_control", StringComparison.Ordinal))
        {
            return "FUNCTION";
        }
        return "FUNCTION";
    }

    private static string InferSize(string id)
    {
        if (id.EndsWith("_yl", StringComparison.Ordinal) || id.Contains("_yl_", StringComparison.Ordinal)
            || id.Contains("youliang", StringComparison.OrdinalIgnoreCase))
        {
            return ModuleSize.Youliang;
        }
        if (id.EndsWith("_xl", StringComparison.Ordinal) || id.Contains("_xl_", StringComparison.Ordinal))
        {
            return ModuleSize.ExtraLarge;
        }
        if (id.EndsWith("_s", StringComparison.Ordinal))
        {
            return ModuleSize.Small;
        }
        if (id.EndsWith("_m", StringComparison.Ordinal))
        {
            return ModuleSize.Medium;
        }
        if (id.EndsWith("_l", StringComparison.Ordinal) || id.Contains("_l_", StringComparison.Ordinal))
        {
            return ModuleSize.Large;
        }
        return ModuleSize.Medium;
    }

    private static void ApplyStubStats(ModuleDef m)
    {
        var id = m.moduleId ?? "";
        if (m.slotCategory == "ATTACK")
        {
            if (id.Contains("hybrid_gun", StringComparison.Ordinal))
            {
                if (id.Contains("_xl", StringComparison.Ordinal))
                {
                    m.damagePerTick = 6000f;
                    m.fireCycleSec = 15f;
                }
                else if (id.Contains("_l", StringComparison.Ordinal))
                {
                    m.damagePerTick = 3000f;
                    m.fireCycleSec = 10f;
                }
                else
                {
                    m.damagePerTick = 1000f;
                    m.fireCycleSec = 10f;
                }
            }
            else
            {
                m.damagePerTick = id.Contains("_xl", StringComparison.Ordinal) ? 140f
                    : id.Contains("_l", StringComparison.Ordinal) ? 95f : 55f;
            }
        }
        else if (m.slotCategory == "DEFENSE")
        {
            if (id.Contains("shield_regen", StringComparison.Ordinal))
            {
                m.shieldRegenPerSec = id.Contains("_l", StringComparison.Ordinal) ? 45f : 28f;
            }
            if (id.Contains("armor_regen", StringComparison.Ordinal))
            {
                m.shieldRegenPerSec = 15f;
            }
            if (id.Contains("shield_resist", StringComparison.Ordinal))
            {
                m.shieldResistPct = id.Contains("_l", StringComparison.Ordinal) ? 16f : 10f;
            }
            if (id.Contains("armor_resist", StringComparison.Ordinal))
            {
                m.armorResistPct = id.Contains("_l", StringComparison.Ordinal) ? 14f : 9f;
            }
        }
        else
        {
            if (id.Contains("propulsion", StringComparison.Ordinal))
            {
                m.speedBonusMps = id.Contains("_l", StringComparison.Ordinal) ? 55f : 35f;
                m.speedBonusPctWhenEnabled = id.Contains("_l", StringComparison.Ordinal) ? 0.15f : 0.1f;
                m.appliesToPropulsion = true;
            }
            if (id.Contains("warp_speed", StringComparison.Ordinal))
            {
                m.speedBonusPctWhenEnabled = 0.08f;
            }
        }
        if (IsMissileModuleId(id))
        {
            m.damagePerTick = 0f;
            m.fireCycleSec = 0f;
            if (m.missileStructureHp <= 0f)
            {
                m.missileStructureHp = 1000f;
            }
            if (m.missileFlightSpeedMps <= 0f)
            {
                m.missileFlightSpeedMps = 1000f;
            }
            if (m.missileFlightMaxSec <= 0f)
            {
                m.missileFlightMaxSec = 30f;
            }
            if (m.missileContactHoldSec <= 0f)
            {
                m.missileContactHoldSec = 1f;
            }
            if (m.missileAoeBaseDamage <= 0f)
            {
                m.missileAoeBaseDamage = 10000f;
            }
            if (m.missileAoeZeroRadiusM <= 0f)
            {
                m.missileAoeZeroRadiusM = 7000f;
            }
            return;
        }
        if (id.StartsWith("plug_speed", StringComparison.Ordinal))
        {
            m.speedBonusMps = 20f;
        }
    }

    public static bool IsTechnicalName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && (name.StartsWith("mod_", StringComparison.Ordinal)
            || name.StartsWith("plug_", StringComparison.Ordinal)
            || name.StartsWith("hull_", StringComparison.Ordinal)
            || name.StartsWith("res_", StringComparison.Ordinal)
            || name.StartsWith("item_", StringComparison.Ordinal));

    public static string DisplayNameZh(ModuleDef? mod)
    {
        if (mod == null)
        {
            return "?";
        }
        var zh = mod.displayName?.Trim();
        if (!string.IsNullOrWhiteSpace(zh) && !IsTechnicalName(zh))
        {
            return zh;
        }
        return mod.moduleId != null ? DisplayNameZh(mod.moduleId) : (zh ?? "?");
    }

    public static string DisplayNameEn(ModuleDef? mod)
    {
        if (mod == null)
        {
            return "";
        }
        var en = mod.displayNameEn?.Trim();
        if (!string.IsNullOrWhiteSpace(en) && !IsTechnicalName(en))
        {
            return en;
        }
        return mod.moduleId != null ? DisplayNameEn(mod.moduleId) : (en ?? "");
    }

    public static string BilingualLabel(ModuleDef? mod)
    {
        if (mod == null)
        {
            return "?";
        }
        var sizeTag = ModuleSize.DisplayTag(mod.moduleSize);
        var zh = DisplayNameZh(mod) + sizeTag;
        var en = DisplayNameEn(mod);
        if (string.IsNullOrWhiteSpace(en) || en.Equals(DisplayNameZh(mod), StringComparison.Ordinal))
        {
            return zh;
        }
        return zh + " / " + en;
    }

    public static string DisplayNameZh(string id) => StubDisplayZh(id);

    public static string DisplayNameEn(string id) => StubDisplayEn(id);

    private static string StubDisplayZh(string id)
    {
        if (id.Contains("hybrid_gun", StringComparison.Ordinal))
        {
            return "混合器";
        }
        if (id.Contains("shield_regen", StringComparison.Ordinal))
        {
            return "盾回模块";
        }
        if (id.Contains("armor_regen", StringComparison.Ordinal))
        {
            return "甲回模块";
        }
        if (id.Contains("shield_resist", StringComparison.Ordinal))
        {
            return "盾抗模块";
        }
        if (id.Contains("armor_resist", StringComparison.Ordinal))
        {
            return "甲抗模块";
        }
        if (id.Contains("propulsion", StringComparison.Ordinal))
        {
            return "推进器";
        }
        if (id.Contains("scanner", StringComparison.Ordinal))
        {
            return "扫描器";
        }
        if (id.Contains("damage_control", StringComparison.Ordinal))
        {
            return "损伤控制";
        }
        if (id.Contains("energy_disrupt", StringComparison.Ordinal))
        {
            return "能量扰断器";
        }
        if (id.Contains("energy_drain", StringComparison.Ordinal))
        {
            return "能量虹吸器";
        }
        if (id.Contains("warp_scram", StringComparison.Ordinal))
        {
            return "跃迁扰断器";
        }
        if (id.Contains("web", StringComparison.Ordinal) && !id.Contains("webber", StringComparison.Ordinal))
        {
            return "跃迁抑制器";
        }
        if (id.Contains("warp_speed", StringComparison.Ordinal))
        {
            return "跃迁加速";
        }
        if (id.Contains("strike_wing", StringComparison.Ordinal))
        {
            return "攻击编队";
        }
        if (id.Contains("chaos_missile", StringComparison.Ordinal))
        {
            return "混沌导弹";
        }
        if (id.Contains("ore_mining", StringComparison.Ordinal) || id.Contains("mining_beam", StringComparison.Ordinal))
        {
            return "采矿光束";
        }
        if (id.StartsWith("plug_", StringComparison.Ordinal))
        {
            return "增益插件";
        }
        return HumanizeModIdZh(id);
    }

    private static string HumanizeModIdZh(string id)
    {
        if (IsTechnicalName(id))
        {
            var body = id;
            if (body.StartsWith("mod_", StringComparison.Ordinal))
            {
                body = body[4..];
            }
            if (body.EndsWith("_s", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            else if (body.EndsWith("_m", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            else if (body.EndsWith("_l", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            body = body.Replace('_', ' ');
            return string.IsNullOrWhiteSpace(body) ? id : body;
        }
        return id;
    }

    private static string StubDisplayEn(string id)
    {
        if (id.Contains("hybrid_gun", StringComparison.Ordinal))
        {
            return "Hybrid Gun";
        }
        if (id.Contains("shield_regen", StringComparison.Ordinal))
        {
            return "Shield Regen";
        }
        if (id.Contains("armor_regen", StringComparison.Ordinal))
        {
            return "Armor Regen";
        }
        if (id.Contains("shield_resist", StringComparison.Ordinal))
        {
            return "Shield Resist";
        }
        if (id.Contains("armor_resist", StringComparison.Ordinal))
        {
            return "Armor Resist";
        }
        if (id.Contains("propulsion", StringComparison.Ordinal))
        {
            return "Propulsion";
        }
        if (id.Contains("scanner", StringComparison.Ordinal))
        {
            return "Scanner";
        }
        if (id.Contains("damage_control", StringComparison.Ordinal))
        {
            return "Damage Control";
        }
        if (id.Contains("energy_disrupt", StringComparison.Ordinal))
        {
            return "Energy Disruptor";
        }
        if (id.Contains("energy_drain", StringComparison.Ordinal))
        {
            return "Energy Drain";
        }
        if (id.Contains("warp_scram", StringComparison.Ordinal))
        {
            return "Warp Scrambler";
        }
        if (id.Contains("web", StringComparison.Ordinal))
        {
            return "Stasis Web";
        }
        if (id.Contains("warp_speed", StringComparison.Ordinal))
        {
            return "Warp Speed";
        }
        if (id.Contains("strike_wing", StringComparison.Ordinal))
        {
            return "Strike Wing";
        }
        if (id.Contains("chaos_missile", StringComparison.Ordinal))
        {
            return "Chaos Missile";
        }
        if (id.Contains("ore_mining", StringComparison.Ordinal) || id.Contains("mining_beam", StringComparison.Ordinal))
        {
            return "Mining Beam";
        }
        if (id.StartsWith("plug_", StringComparison.Ordinal))
        {
            return "Stat Plugin";
        }
        return HumanizeModIdEn(id);
    }

    private static string HumanizeModIdEn(string id)
    {
        if (IsTechnicalName(id))
        {
            var body = id;
            if (body.StartsWith("mod_", StringComparison.Ordinal))
            {
                body = body[4..];
            }
            if (body.EndsWith("_s", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            else if (body.EndsWith("_m", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            else if (body.EndsWith("_l", StringComparison.Ordinal))
            {
                body = body[..^2];
            }
            return body.Replace('_', ' ');
        }
        return id;
    }
}
