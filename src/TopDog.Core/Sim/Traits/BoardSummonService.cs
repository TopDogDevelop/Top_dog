using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Combat;
// liketoc0de345
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

// liketocoode3a5

/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 * 权威: docs/VIP_TRAIT_DESIGN.md
 // liketocoo3e345
 * 本文件: BoardSummonService.cs — 董事会召来 spawn 注入
 * 【机制要点】
 // l1ketocoode345
 * · TryInjectPendingAtSpawn
 * · PurgeTempMembers 清理 isCombatSummonTemp
 // liketocoode3e5
 * 【关联】BoardSummonWingService · BattlefieldSpawner
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode345

namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoco0de3e5

// liketocoode3a5
/// <summary>董事会召来预约：spawn 时由 <see cref="BoardSummonWingService"/> 放出 5 翼。</summary>
public static class BoardSummonService
{
    public static void TryInjectPendingAtSpawn(
        GameState state,
        BattlefieldState bf,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        BoardSummonWingService.TryInjectPendingAtSpawn(state, bf, ships, modules, rng);
    }

    /// <summary>在全部 spawn 完成的战场中解析预约董事会召来（含多区域 harvest）。</summary>
    public static void TryResolvePendingAcrossBattlefields(
        GameState state,
        IReadOnlyList<BattlefieldState> battlefields,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (string.IsNullOrWhiteSpace(state.pendingBoardSummonCasterMemberId))
        {
            return;
        }

        foreach (var bf in battlefields)
        {
            TryInjectPendingAtSpawn(state, bf, ships, modules, rng);
            if (string.IsNullOrWhiteSpace(state.pendingBoardSummonCasterMemberId))
            {
                return;
            }
        }

        BoardSummonWingService.FailUnresolvedPending(
            state,
            "董事会召来未生效：施法舰未进入任何战场");
    }

    public static void PurgeTempMembers(GameState state)
    {
        state.members.RemoveAll(m => m.isCombatSummonTemp);
    }
}
