namespace TopDog.Client.Tactical;

/// <summary>实时战主视口模式（战术视野 ↔ 战略星图）。</summary>
public enum CombatViewMode
{
    Tactical,
    StarMap,
}

public static class CombatViewModeState
{
    public static CombatViewMode Mode { get; private set; } = CombatViewMode.Tactical;

    public static event System.Action? ModeChanged;

    public static void Set(CombatViewMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        ModeChanged?.Invoke();
    }

    public static void Toggle() =>
        Set(Mode == CombatViewMode.Tactical ? CombatViewMode.StarMap : CombatViewMode.Tactical);
}
