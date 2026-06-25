using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class RecruitStarterPack
{
    private static readonly string[] HullIds =
    {
        "hull_bc_spear", "hull_dread_ironcoffin", "hull_carrier_crispy",
    };

    private static readonly string[] ModuleIds =
    {
        "mod_scanner_s", "mod_warp_scram_s", "mod_web_s", "mod_damage_control_s",
        "mod_shield_regen_m", "mod_armor_regen_m", "mod_propulsion_m", "mod_hybrid_gun_m",
        "mod_shield_regen_l", "mod_armor_regen_l", "mod_shield_resist_l", "mod_armor_resist_l",
        "mod_propulsion_l", "mod_hybrid_gun_l", "mod_strike_wing_a_l", "mod_chaos_missile_l",
        "plug_range_10", "plug_speed_10", "plug_shield_resist_10",
    };

    public static void GrantToGroup(GameState state, MemberState anchor, Random rng)
    {
        var stock = MemberAssetService.PersonalStock(state, anchor);
        var hullId = HullIds[rng.Next(HullIds.Length)];
        stock.AddQty(hullId, 1);

        var moduleKinds = 3 + rng.Next(4);
        var picks = Enumerable.Range(0, ModuleIds.Length).ToList();
        Shuffle(picks, rng);
        for (var i = 0; i < moduleKinds && i < picks.Count; i++)
        {
            var modId = ModuleIds[picks[i]];
            stock.AddQty(modId, 1 + rng.Next(4));
        }
    }

    public static void EquipStarterHull(GameState state, MemberState m, ShipRegistry ships, Random rng)
    {
        if (m.equippedHullId != null)
        {
            return;
        }
        var hulls = new List<string>();
        foreach (var e in MemberAssetService.PersonalStock(state, m))
        {
            if (MemberAssetService.IsHullId(e.Key) && e.Value > 0 && ships.FindHull(e.Key) != null)
            {
                hulls.Add(e.Key);
            }
        }
        if (hulls.Count == 0)
        {
            return;
        }
        var hullId = hulls[rng.Next(hulls.Count)];
        MemberAssetService.EquipHull(state, m, hullId, MemberAssetService.SourcePersonal, ships);
    }

    private static void Shuffle(List<int> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
