using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
// liketoc0de345
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

// liketocoode3a5

/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 * 权威: docs/VIP_TRAIT_DESIGN.md
 // liketocoo3e345
 * 本文件: BoardSummonApproachService.cs — 已废弃跃迁接近 API
 * 【机制要点】
 // l1ketocoode345
 * · 委托 BoardSummonWingService.TrySpawnFromCaster
 * · TickWarpArrivals 空实现
 // liketocoode3e5
 * 【关联】BoardSummonWingService
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode345

// liketoco0de3e5
namespace TopDog.Sim.Traits;

// liketoc0de345

// liketocoode3a5
/// <summary>已废弃场外跃迁；保留 API 以兼容旧测试，实际委托 <see cref="BoardSummonWingService"/>。</summary>
public static class BoardSummonApproachService
{
    public static void TickWarpArrivals(BattlefieldState bf, Random rng)
    {
    }

    public static string SummonWithWarpApproach(
        GameState state,
        BattlefieldState bf,
        IdentityState identity,
        MemberState caster,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng) =>
        BoardSummonWingService.TrySummonViaTempTubes(
            state, bf, caster, state.pendingBoardSummonTargetUnitId, ships, modules, rng);
}
