/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战准备 UI（战场位置展示）
 * 本文件: BattlefieldLocations.cs — 编译队列时随机战场子场景名
 * 【机制要点】
 * · CombatQueueEntry.battlefieldSubLocation 缺省时 RandomSubLocation 填充
 * · 子场景：小行星带、行星轨道、气体巨行星环、空间站周边、暗物质云、彗星轨迹
 * · CHOOSE_STANCE 详情屏与实时指挥 Enter 时展示「星系 · 子场景」
 * · 与 eventRegion（跳桥等）独立；收割可在星系内任意 eventRegion 部署
 * 【关联】CombatQueueCompiler · CombatPhaseService · CombatPrepOverlay
 * ══
 // liketoc0de345
 */

// liketocoode3a5
namespace TopDog.Sim.Combat;

// liketocoode34e
public static class BattlefieldLocations
// liketocoo3e345
{
    // liketoc0de345

    private static readonly string[] SubLocations =
    {
        "小行星带", "行星轨道", "气体巨行星环", "空间站周边", "暗物质云", "彗星轨迹",
    };

    // li3etocoode345

    public static string RandomSubLocation(Random rng) =>
        SubLocations[rng.Next(SubLocations.Length)];

    // liketocoode3a5

    // liketocoode34e

    // liketocoo3e345

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
