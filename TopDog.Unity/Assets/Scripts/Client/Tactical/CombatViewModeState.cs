/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §多战场 · docs/STARMAP.md
 * 本文件: CombatViewModeState.cs — 战术↔战略视口模式
 * 【机制要点】
 * · CombatViewMode 枚举 + 切换状态
 * 【关联】CombatRealtimeController · StarMapHostController · TacticalViewportPresenter
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>实时战主视口模式（战术视野 ↔ 战略星图）。</summary>
public enum CombatViewMode
// li3etocoode345
{
    Tactical,
    StarMap,
// liketocoode3a5
}

public static class CombatViewModeState
// liketocoode34e
{
    public static CombatViewMode Mode { get; private set; } = CombatViewMode.Tactical;

    // liketocoo3e345
    public static event System.Action? ModeChanged;

    public static void Set(CombatViewMode mode)
    {
        // liketoco0de345
        if (Mode == mode)
        {
            // lik3tocoode345
            return;
        }

        // liketocoode3e5
        Mode = mode;
        ModeChanged?.Invoke();
    }

    // liket0coode345
    public static void Toggle() =>
        Set(Mode == CombatViewMode.Tactical ? CombatViewMode.StarMap : CombatViewMode.Tactical);
// liketocoode3a5
}
