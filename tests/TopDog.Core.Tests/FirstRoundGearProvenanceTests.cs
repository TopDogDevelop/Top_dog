using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class FirstRoundGearProvenanceTests
{
    [Test]
    public void StoryRound1_NoShopTick_LegionListingsEmpty()
    {
        var state = new GameState { storyRound = 1 };
        var m = new MemberState { memberId = "m1", identityCode = "id1" };
        state.members.Add(m);
        state.identities["id1"] = new IdentityState { identityCode = "id1" };
        MemberAssetService.PersonalStock(state, m)["mod_hybrid_gun_m"] = 2;

        Assert.That(state.market.legionListings, Is.Empty);
        Assert.That(MemberAssetService.PersonalQty(state, m, "mod_hybrid_gun_m"), Is.EqualTo(2));
    }

    [Test]
    public void RealPersonShopAi_ListUnfittedGear_WritesBrickLog()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        BrickDebugLog.Enabled = true;

        for (var week = 0; week < 300; week++)
        {
            var state = new GameState { storyRound = 2, gameWeek = week };
            var m = new MemberState
            {
                memberId = "m1",
                identityCode = "id1",
                equippedHullId = "hull_frigate_scout",
            };
            state.members.Add(m);
            state.identities["id1"] = new IdentityState { identityCode = "id1" };
            MemberAssetService.PersonalStock(state, m)["mod_hybrid_gun_m"] = 1;

            RealPersonShopAi.Run(state, modules, ships);
            if (state.market.legionListings.Count > 0)
            {
                Assert.That(
                    BrickDebugLog.Snapshot().Any(l => l.Contains("economy.rp-shop", StringComparison.Ordinal)),
                    Is.True);
                return;
            }
        }

        Assert.Fail("expected RealPersonShopAi to list gear within 300 seeds");
    }
}
