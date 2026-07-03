using TopDog.Content.Banter;
using TopDog.Content.Map;
using TopDog.Foundation.Io;
using TopDog.Sim.Banter;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BanterDynamicTextTests
{
    [SetUp]
    public void SetUp() => BanterDynamicTextResolver.ResetCachesForTests();

    [Test]
    public void Resolve_ReplacesOwnedSite_FromPlayerBuildings()
    {
        var project = new MapProject
        {
            systems =
            {
                new SolarSystemDef { solarSystemId = "sys_a", name = "阿尔法" },
            },
        };
        var state = new GameState
        {
            map = new LoadedMap(project, null),
            banterRuntime = new MemberBanterRuntimeState(),
        };
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_a",
            playerOwned = true,
            status = BuildingService.Normal,
        });

        var text = BanterDynamicTextResolver.Resolve(
            state,
            state.banterRuntime,
            "人在（随机地点）等你们",
            new Random(1));

        Assert.That(text, Is.EqualTo("人在阿尔法等你们"));
        Assert.That(state.banterRuntime.idleDynamicContext!.OwnedSite, Is.EqualTo("阿尔法"));
    }

    [Test]
    public void Resolve_ReusesSameOwnedSiteWithinRound()
    {
        var project = new MapProject
        {
            systems =
            {
                new SolarSystemDef { solarSystemId = "sys_a", name = "阿尔法" },
            },
        };
        var state = new GameState
        {
            map = new LoadedMap(project, null),
            banterRuntime = new MemberBanterRuntimeState(),
        };
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_a",
            playerOwned = true,
            status = BuildingService.Normal,
        });

        var rt = state.banterRuntime;
        var rng = new Random(1);
        var first = BanterDynamicTextResolver.Resolve(state, rt, "在（随机地点）A", rng);
        var second = BanterDynamicTextResolver.Resolve(state, rt, "去（随机地点）B", rng);

        Assert.That(first, Is.EqualTo("在阿尔法A"));
        Assert.That(second, Is.EqualTo("去阿尔法B"));
    }

    [Test]
    public void Resolve_RollsIndependentModulesPerToken()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")));
        BanterDynamicTextResolver.ResetCachesForTests();

        var foundDistinct = false;
        for (var seed = 0; seed < 80; seed++)
        {
            var state = new GameState
            {
                legionStock = { ["mod_boarding_s"] = 99 },
                banterRuntime = new MemberBanterRuntimeState(),
            };
            var text = BanterDynamicTextResolver.Resolve(
                state,
                state.banterRuntime,
                "（随机装备）|（随机装备）",
                new Random(seed));
            var parts = text.Split('|');
            if (parts.Length == 2 && !string.Equals(parts[0], parts[1], StringComparison.Ordinal))
            {
                foundDistinct = true;
                break;
            }
        }

        Assert.That(foundDistinct, Is.True);
    }

    [Test]
    public void RollModule_UsesFullCatalog_WhenStockOnlyHasBoarding()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")));
        BanterDynamicTextResolver.ResetCachesForTests();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var seed = 0; seed < 40; seed++)
        {
            var state = new GameState
            {
                legionStock = { ["mod_boarding_s"] = 5 },
                banterRuntime = new MemberBanterRuntimeState(),
            };
            var text = BanterDynamicTextResolver.Resolve(
                state,
                state.banterRuntime,
                BanterDynamicTextResolver.ModuleToken,
                new Random(seed));
            seen.Add(text);
        }

        Assert.That(seen.Count, Is.GreaterThan(1), "全表抽样时不应永远只有登录模块");
    }

    [Test]
    public void RosterSpeakers_AssignsThreeDistinctSlots_FromPool()
    {
        var state = new GameState
        {
            legions = { new LegionState { legionId = "L1", isLocal = true } },
            banterRuntime = new MemberBanterRuntimeState(),
            members =
            {
                new MemberState { memberId = "1000000301", identityCode = "10000003", name = "丙" },
                new MemberState { memberId = "1000000101", identityCode = "10000001", name = "甲" },
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "乙" },
            },
        };
        foreach (var m in state.members)
        {
            m.legionId = "L1";
        }

        BanterRosterSpeakers.PrepareRound(state, state.banterRuntime, new Random(7));

        var slots = new[]
        {
            BanterRosterSpeakers.ResolveSlot(state.banterRuntime, "@1"),
            BanterRosterSpeakers.ResolveSlot(state.banterRuntime, "@2"),
            BanterRosterSpeakers.ResolveSlot(state.banterRuntime, "@3"),
        };
        Assert.That(slots.Distinct().Count(), Is.EqualTo(3));
        Assert.That(slots, Does.Not.Contain(null));
    }

    [Test]
    public void Catalog_LoadsScriptGroups()
    {
        var catalog = BanterCatalogLoader.LoadDefault();
        Assert.That(catalog.IdleGroups.ContainsKey("idle_chat_01"), Is.True);
        Assert.That(catalog.IdleGroups["idle_chat_08"].Count, Is.EqualTo(4));
    }

    [Test]
    public void OrderByMemberCode_SortsAscending()
    {
        var list = new List<MemberState>
        {
            new() { memberId = "1000000301" },
            new() { memberId = "1000000101" },
            new() { memberId = "1000000201" },
        };
        var sorted = MemberRosterSort.OrderByMemberCode(list);
        Assert.That(sorted.Select(m => m.memberId), Is.EqualTo(new[] { "1000000101", "1000000201", "1000000301" }));
    }
}
