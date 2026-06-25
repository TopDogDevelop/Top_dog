using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class LegionQueryTests
{
    [Test]
    public void IsLocalLegion_MapsLegacyPlayerIdToLobbyLegion()
    {
        var state = new GameState();
        var localId = "host-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        Assert.That(LegionQuery.IsLocalLegion(state, CampaignLegionIds.Player), Is.True);
        Assert.That(LegionQuery.IsLocalLegion(state, localId), Is.True);
        Assert.That(LegionQuery.IsHostileLegion(state, "ai-uuid"), Is.True);
    }

    [Test]
    public void IsLocalMember_ExcludesAiRoster()
    {
        var state = new GameState();
        var localId = "host-uuid";
        var aiId = "ai-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.legions.Add(new LegionState { legionId = aiId, isAiControlled = true });
        var local = new MemberState { memberId = "p1", legionId = localId, isPlayer = true };
        var ai = new MemberState { memberId = "a1", legionId = aiId, isAi = true };
        Assert.That(LegionQuery.IsLocalMember(state, local), Is.True);
        Assert.That(LegionQuery.IsLocalMember(state, ai), Is.False);
        Assert.That(LegionQuery.IsHostileLegion(state, aiId), Is.True);
    }
}

public sealed class SecurityBandsTests
{
    [Test]
    public void ColorForSecurity_CoversLowBandGap()
    {
        var bands = new SecurityBands();
        bands.bands.Add(new SecurityBands.Band { minSecurity = 0.5f, maxSecurity = 1f, uiColor = "#4a9eff" });
        bands.bands.Add(new SecurityBands.Band { minSecurity = 0.01f, maxSecurity = 0.49f, uiColor = "#e6a817" });
        bands.bands.Add(new SecurityBands.Band { minSecurity = -1f, maxSecurity = 0f, uiColor = "#ff4444" });
        Assert.That(bands.ColorForSecurity(0.05f), Is.EqualTo("#e6a817"));
        Assert.That(bands.ColorForSecurity(0f), Is.EqualTo("#ff4444"));
    }
}
