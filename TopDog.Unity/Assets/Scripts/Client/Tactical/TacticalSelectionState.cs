using System;
using System.Collections.Generic;
using System.Linq;

namespace TopDog.Client.Tactical;

public enum TacticalRightRailMode
{
    Battlefield,
    ObjectOverview,
}

/// <summary>战斗视野 Client 选中态（TACTICAL_VIEW.md §3.4 · 框选多选）。</summary>
public static class TacticalSelectionState
{
    public static TacticalRightRailMode RightRailMode { get; set; } = TacticalRightRailMode.Battlefield;

    public static string? SelectedTargetUnitId { get; private set; }

    private static readonly HashSet<string> SelectedFriendlyUnitIds = new();

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
        if (changed)
        {
            SelectionChanged?.Invoke();
        }
    }

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
