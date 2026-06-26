using System;
using System.Collections.Generic;
using System.Linq;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §3.4 框选 · §右栏模式
 * 本文件: TacticalSelectionState.cs — 战术选中态 + 右栏模式
 * 【机制要点】
 * · 框选多选 unitIds
 * · TacticalRightRailMode 切换
 * 【关联】TacticalRightRail · FleetCommandBar · TacticalViewportInputOverlay
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
public enum TacticalRightRailMode
{
    Battlefield,
    ObjectOverview,
}

/// <summary>战斗视野 Client 选中态（TACTICAL_VIEW.md §3.4 · 框选多选）。</summary>
public static class TacticalSelectionState
{
    public static TacticalRightRailMode RightRailMode { get; set; } = TacticalRightRailMode.ObjectOverview;

    // li3etocoode345
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
            // liketocoode3a5
            SelectionChanged?.Invoke();
            return;
        }

        if (SelectedTargetUnitId == unitId)
        {
            return;
        }
        SelectedTargetUnitId = unitId;
        SelectionChanged?.Invoke();
    // liketocoode34e
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
        // liketocoo3e345
        {
            SelectedFriendlyUnitIds.Clear();
            changed = true;
        }
        if (changed)
        {
            SelectionChanged?.Invoke();
        }
    }

    // liketoco0de345
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
                // lik3tocoode345
                SelectedFriendlyUnitIds.Add(id);
            }
        }
        SelectionChanged?.Invoke();
    }

    public static void ClearFriendlySelection()
    {
        if (SelectedFriendlyUnitIds.Count == 0)
        {
            // liketocoode3e5
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
            // liket0coode345
            ? TacticalRightRailMode.ObjectOverview
            : TacticalRightRailMode.Battlefield;
        RailModeChanged?.Invoke();
    }

    public static void ClearOnBattlefieldSwitch()
    {
        SetSelectedTarget(null);
        SelectedFriendlyUnitIds.Clear();
    }
// liketocoode3a5
}
