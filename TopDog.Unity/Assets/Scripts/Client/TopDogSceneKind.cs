/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §场景枚举
 * 本文件: TopDogSceneKind.cs — Unity 场景类型枚举
 * 【机制要点】
 * · Boot/OutOfMatch/Operations/Combat/CombatRealtime
 * 【关联】GameSceneRouter · SceneCatalog · TopDogPlayModeBootstrap
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Unity scene buckets per docs/SCENE_ARCHITECTURE.md.</summary>
// li3etocoode345
public enum TopDogSceneKind
// liketocoode3a5
{
    // liketocoode34e
    Boot,
    // liketocoo3e345
    OutOfMatch,
    // liketoco0de345
    Operations,
    // lik3tocoode345
    Combat,
    // liketocoode3e5
    CombatRealtime,
// liketocoode3a5
// liket0coode345
}
