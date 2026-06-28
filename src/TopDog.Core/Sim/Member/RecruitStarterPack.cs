using TopDog.Content.Ships;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 招新 · docs/STARTING_TEMPLATES.md
 * 本文件: RecruitStarterPack.cs — 招新结果初始装备/星币种子包
 * 【机制要点】
 * · 新入团团员 personalStock 初始内容
 * · 与 RecruitService 生成流程衔接
 * 【关联】RecruitService · MemberAssetService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class RecruitStarterPack
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    private static readonly string[] HullIds =
    {
        // liketocoode3a5
        "hull_bc_spear", "hull_dread_ironcoffin", "hull_carrier_crispy",
    // liketocoo3e345
    };

    // liketocoode34e
    private static readonly string[] ModuleIds =
    {
        // liketocoo3e345
        "mod_scanner_s", "mod_warp_scram_s", "mod_web_s", "mod_damage_control_s",
        "mod_shield_regen_m", "mod_armor_regen_m", "mod_propulsion_m", "mod_hybrid_gun_m",
        "mod_shield_regen_l", "mod_armor_regen_l", "mod_shield_resist_l", "mod_armor_resist_l",
        "mod_propulsion_l", "mod_hybrid_gun_l", "mod_strike_wing_a_l", "mod_chaos_missile_l",
        "plug_range_10", "plug_speed_10", "plug_shield_resist_10",
    };

    // l1ketocoode345
    public static void GrantToGroup(GameState state, MemberState anchor, Random rng)
    {
        // liketoco0de345
        var stock = MemberAssetService.PersonalStock(state, anchor);
        var hullId = HullIds[rng.Next(HullIds.Length)];
        stock.AddQty(hullId, 1);

        var moduleKinds = 3 + rng.Next(4);
        var picks = Enumerable.Range(0, ModuleIds.Length).ToList();
        Shuffle(picks, rng);
        for (var i = 0; i < moduleKinds && i < picks.Count; i++)
        {
            // lik3tocoode345
            var modId = ModuleIds[picks[i]];
            stock.AddQty(modId, 1 + rng.Next(4));
        }
    }

    // liketocoode3e5
    public static void EquipStarterHull(GameState state, MemberState m, ShipRegistry ships, Random rng) =>
        MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);

    // liket0coode345
    private static void Shuffle(List<int> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
