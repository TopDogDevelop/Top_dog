using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.Sim.Realtime;

namespace TopDog.Client.Tactical;

public enum TacticalRightRailMode
{
    Battlefield,
    ObjectOverview,
}

/// <summary>战斗视野 Client 选中态（TACTICAL_VIEW.md §3.4 · 框选多选）。</summary>
public static class TacticalSelectionState
{
    public static TacticalRightRailMode RightRailMode { get; set; } = TacticalRightRailMode.ObjectOverview;

    public static string? SelectedTargetUnitId { get; private set; }

    private static readonly HashSet<string> SelectedFriendlyUnitIds = new();

    /// <summary>底栏「默认距离」（km）；0=不限距。跨对局由 ClientGameSettings/PlayerPrefs 记忆。</summary>
    public static float DefaultCommandRangeKm { get; private set; }

    private static bool _defaultRangeLoaded;

    /// <summary>下令用的有效默认距离（km）；0=不限距。</summary>
    public static float EffectiveDefaultCommandRangeKm
    {
        get
        {
            EnsureDefaultCommandRangeLoaded();
            return DefaultCommandRangeKm;
        }
    }

    /// <summary>从 PlayerPrefs 载入（启动 / 底栏绑定）。</summary>
    public static void EnsureDefaultCommandRangeLoaded()
    {
        if (_defaultRangeLoaded)
        {
            return;
        }

        DefaultCommandRangeKm = global::TopDog.Client.ClientGameSettings.DefaultCommandRangeKm;
        _defaultRangeLoaded = true;
    }

    /// <summary>松手提交或外部写入已持久化值。</summary>
    public static void ApplyPersistedDefaultCommandRangeKm(float km)
    {
        DefaultCommandRangeKm = Math.Clamp(km, TacticalRangeScale.MinKm, TacticalRangeScale.MaxKm);
        _defaultRangeLoaded = true;
    }

    /// <summary>无框选时命令范围（同步至 GameState.fleetCommandScope）。</summary>
    public static FleetCommandScope CommandScope { get; set; } = FleetCommandScope.AllInScene;

    /// <summary>指针悬停单位（战术视口拾取）。</summary>
    public static string? HoveredUnitId { get; set; }

    public static event System.Action? SelectionChanged;
    public static event System.Action? RailModeChanged;

    public static IReadOnlyCollection<string> GetSelectedFriendlyUnitIds() => SelectedFriendlyUnitIds;

    public static void SetSelectedTarget(string? unitId)
    {
        if (unitId != null && unitId.Equals(SelectedTargetUnitId, StringComparison.Ordinal))
        {
            SelectedTargetUnitId = null;
            SelectionChanged?.Invoke();
            return;
        }

        if (SelectedTargetUnitId == unitId)
        {
            return;
        }
        SelectedTargetUnitId = unitId;
        SelectionChanged?.Invoke();
    }

    public static void ClearTargetAndBoxSelection()
    {
        var changed = false;
        if (SelectedTargetUnitId != null)
        {
            SelectedTargetUnitId = null;
            changed = true;
        }
        if (SelectedFriendlyUnitIds.Count > 0)
        {
            SelectedFriendlyUnitIds.Clear();
            changed = true;
        }
        CommandScope = FleetCommandScope.AllInScene;
        if (changed)
        {
            SelectionChanged?.Invoke();
        }
    }

    public static string CommandScopeLabel() =>
        CommandScope == FleetCommandScope.AllInScene ? "命令范围：当前场景内所有" : "命令范围：仅选中";

    public static void SetBoxSelection(IEnumerable<string> friendlyUnitIds, bool additive)
    {
        if (!additive)
        {
            SelectedFriendlyUnitIds.Clear();
        }
        foreach (var id in friendlyUnitIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                SelectedFriendlyUnitIds.Add(id);
            }
        }
        SelectionChanged?.Invoke();
    }

    public static void ClearFriendlySelection()
    {
        if (SelectedFriendlyUnitIds.Count == 0)
        {
            return;
        }
        SelectedFriendlyUnitIds.Clear();
        SelectionChanged?.Invoke();
    }

    public static bool IsFriendlySelected(string? unitId) =>
        unitId != null && SelectedFriendlyUnitIds.Contains(unitId);

    public static void ToggleRailMode()
    {
        RightRailMode = RightRailMode == TacticalRightRailMode.Battlefield
            ? TacticalRightRailMode.ObjectOverview
            : TacticalRightRailMode.Battlefield;
        RailModeChanged?.Invoke();
    }

    public static void ClearOnBattlefieldSwitch()
    {
        SetSelectedTarget(null);
        SelectedFriendlyUnitIds.Clear();
    }
}
