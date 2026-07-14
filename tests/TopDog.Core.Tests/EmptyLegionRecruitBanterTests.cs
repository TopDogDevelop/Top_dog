using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Banter;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

/// <summary>
/// Repro: 无团员开局 → 招新入队 → 左侧 companionLog 是否产出闲聊。
/// </summary>
[TestFixture]
public sealed class EmptyLegionRecruitBanterTests
{
    [Test]
    public void AfterExchangeRecruit_IdleBanterEmitsCompanionLines()
    {
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
            banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 0f },
        };
        state.flags["exchange.enabled"] = "1";
        state.legions.Add(new LegionState
        {
            legionId = "legion_local",
            displayName = "空团",
            isLocal = true,
        });
        LegionPlayerRegistry.EnsureFromLegions(state);
        Assert.That(state.members.Count, Is.EqualTo(0));

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var traits = TraitCatalog.LoadDefault();

        // 无团员时先空转伴聊：不应崩溃；companion 可仍为空
        var banter = new MemberBanterService(TopDog.Content.Banter.BanterCatalogLoader.LoadDefault(), seed: 42);
        for (var i = 0; i < 40; i++)
        {
            banter.Tick(state, 1f, i);
        }

        Assert.That(BanterEligibleSpeakers.List(state).Count, Is.EqualTo(0));
        var emptyPhaseLogCount = state.companionLog.Count;
        Assert.That(emptyPhaseLogCount, Is.EqualTo(0), "空团阶段不应产出 companion 闲聊");

        RecruitService.Start(state, null);
        var recruitBrick = new LegionRecruitBrick("legion_local");
        var ctx = new BrickContext(
            state,
            new TopDog.Foundation.Bus.EventBus(),
            new TopDog.Foundation.Clock.SimClock(),
            ships,
            modules,
            traits,
            new TopDog.Sim.Order.CommandParser());
        // 读条 20s
        for (var i = 0; i < 25; i++)
        {
            recruitBrick.Tick(ctx, 1f);
        }

        Assert.That(state.members.Count, Is.GreaterThan(0), "招新完成后应入队成员");
        Assert.That(BanterEligibleSpeakers.List(state).Count, Is.GreaterThan(0), "招新后应有伴聊候选人");

        var before = state.companionLog.Count;
        // 不复位 idle：0→有人应马上拉起，无需再空等一轮 30s
        var t0 = 40f;
        var firstEmitAt = -1f;
        for (var i = 0; i < 60; i++)
        {
            banter.Tick(state, 1f, t0 + i);
            if (state.companionLog.Count > before)
            {
                firstEmitAt = t0 + i;
                break;
            }
        }

        Assert.That(
            firstEmitAt,
            Is.GreaterThanOrEqualTo(0f),
            $"招新后伴聊应写入 companionLog（空团阶段日志={emptyPhaseLogCount}，当前调度 next={state.banterRuntime!.idleNextEmitSec}）");
        Assert.That(
            firstEmitAt - t0,
            Is.LessThanOrEqualTo(BanterIdleTiming.EmptyRosterPollSec + 2f),
            "招新入队后应尽快出第一句，不应再被空团 RoundGap 卡住");
    }

    [Test]
    public void AfterDirectRecruit_NoExchange_IdleBanterEmitsCompanionLines()
    {
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
            banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 0f },
        };
        state.legions.Add(new LegionState
        {
            legionId = "legion_local",
            displayName = "空团",
            isLocal = true,
        });
        LegionPlayerRegistry.EnsureFromLegions(state);

        var traits = TraitCatalog.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        RecruitService.Start(state, null);
        // 无 exchange：直接读条完成入队
        for (var i = 0; i < 25; i++)
        {
            RecruitService.Tick(state, 1f, traits, new Random(7), ships);
        }

        Assert.That(state.members.Count, Is.GreaterThan(0));

        var banter = new MemberBanterService(TopDog.Content.Banter.BanterCatalogLoader.LoadDefault(), seed: 7);
        state.banterRuntime!.idleNextEmitSec = 0f;
        var before = state.companionLog.Count;
        for (var i = 0; i < 120; i++)
        {
            banter.Tick(state, 1f, i);
            if (state.companionLog.Count > before)
            {
                break;
            }
        }

        Assert.That(state.companionLog.Count, Is.GreaterThan(before));
    }
}
