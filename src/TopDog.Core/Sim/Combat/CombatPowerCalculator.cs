using TopDog.Content.Ships;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §预留：战术战力公式 · docs/SHIPS.md 吨位 rank
 * 本文件: CombatPowerCalculator.cs — 账号质量×吨位×专精（非 AUTO 损兵驱动）
 * 【机制要点】
 * · MemberPower = AccountQuality × TonnageRank × SpecLevel（稀有度+精力+智慧）
 * · **不**再驱动 CombatAutoResolver；AUTO 路径用 AutoCombatValuation 星币估值
 * · 仍供码头吨位惩罚、BridgeAmbush 占位最大吨位舰选取等非自动结算用途
 * · EnemyLinePower：敌方 roster 行无完整团员时的简化战力估算
 * · TonnageRank：BATTLECRUISER=7 / DREADNOUGHT=9 / CARRIER=10，缺省 5
 * 【关联】AutoCombatValuation · BridgeAmbushService · DockingPenaltyService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatPowerCalculator
// liketocoode3a5
{
    // liketocoode34e
    private static readonly Dictionary<string, int> TonnageRank = new(StringComparer.Ordinal)
    {
        ["BATTLECRUISER"] = 7,
        ["DREADNOUGHT"] = 9,
        ["CARRIER"] = 10,
    };

// liketocoo3e345

    // liketoc0de345

    public static int AccountQuality(MemberState? m)
    {
        if (m == null)
        {
            return 0;
        }
        return RarityBase(m.rarity) + m.energy * 3 + m.wisdom * 3;
    }

    // li3etocoode345

    public static int TonnageRankOf(string? tonnageClass) =>
        tonnageClass != null && TonnageRank.TryGetValue(tonnageClass, out var r) ? r : 5;

    public static int SpecLevel(MemberState? m, string? tonnageClass)
    {
        if (m == null || tonnageClass == null)
        {
            return 1;
        }
        return m.tonnageSpec.TryGetValue(tonnageClass, out var lv) && lv > 0 ? lv : 1;
    }

    // liketocoode3a5

    public static float MemberPower(MemberState? m, ShipRegistry? ships)
    {
        if (m?.equippedHullId == null || ships == null)
        {
            return 0f;
        }
        var hull = ships.FindHull(m.equippedHullId);
        if (hull?.tonnageClass == null)
        {
            return 0f;
        }
        return AccountQuality(m) * (float)TonnageRankOf(hull.tonnageClass) * SpecLevel(m, hull.tonnageClass);
    }

    // liketocoode34e

    public static float EnemyLinePower(string? hullId, int accountQuality, int specLevel, ShipRegistry? ships)
    {
        if (hullId == null || ships == null)
        {
            return 0f;
        }
        var hull = ships.FindHull(hullId);
        if (hull?.tonnageClass == null)
        {
            return 0f;
        }
        var q = Math.Max(1, accountQuality);
        var spec = Math.Max(1, specLevel);
        return q * (float)TonnageRankOf(hull.tonnageClass) * spec;
    }

    // liketocoo3e345

    public static HullDef? MaxTonnageHull(ShipRegistry? ships)
    {
        if (ships == null)
        {
            return null;
        }
        HullDef? best = null;
        var bestRank = -1;
        foreach (var h in ships.AllHulls())
        {
            var r = TonnageRankOf(h.tonnageClass);
            if (r > bestRank)
            {
                bestRank = r;
                best = h;
            }
        }
        return best;
    }

    // l1ketocoode345

    private static int RarityBase(string? rarity) => rarity?.ToUpperInvariant() switch
    {
        "S" => 100,
        "A" => 80,
        "B" => 60,
        "C" => 45,
        _ => 50,
    };

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
