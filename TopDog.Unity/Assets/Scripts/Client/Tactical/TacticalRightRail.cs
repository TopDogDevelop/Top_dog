using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.AgentDiag;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §2 · docs/TACTICAL_VIEW.md §3.2
 * 本文件: TacticalRightRail.cs — 右栏双模式切换
 * 【机制要点】
 * · 模式 B：逻辑表无上限；DOM 用固定高度虚拟窗（spacer）浏览全部；步进只填可见窗
 * · 顶部可点吨位图标过滤器；选中只改高亮不整表重建
 * · 模式 A：ListBattlefieldVisionGroups
 * 【关联】TacticalSelectionState · TacticalIconCatalog · VisionLocationService
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
namespace TopDog.Client.Tactical;

/// <summary>右栏双模式：场景图标总览（默认）/ 可观察·附身战场列表（TACTICAL_VIEW.md §3）。</summary>
public sealed class TacticalRightRail
{
    private readonly VisualElement _hostRoot;
    private readonly ScrollView _battlefieldScroll;
    private readonly ScrollView _objectScroll;
    private readonly Button _toggleBtn;
    private readonly VisualElement _battlefieldContent;
    private readonly VisualElement _objectContent;
    private readonly TacticalObjectCommandMenu _objectCommandMenu;
    private readonly Action? _refreshCombatUi;

    private string _lastObjectOverviewKey = "";
    private string _lastBattlefieldRailKey = "";
    private float _nextRailForceRefresh;

    /// <summary>重表现帧向虚拟窗追加的行数（逻辑无上限；DOM 不超过 Visible）。</summary>
    public const int ObjectOverviewStepRows = 12;

    /// <summary>同时挂在树上的舰船行上限（虚拟滚动窗）。</summary>
    public const int ObjectOverviewVisibleShipRows = 48;

    private const float ObjectOverviewRowHeightPx = 28f;
    private const float ObjectOverviewDistanceIntervalSec = 0.75f;

    private string? _tonnageFilter;
    private int _shipWindowStart;
    private int _shipRowsBuiltInWindow;
    private bool _overviewBuilding;
    private bool _scrollHooked;
    private bool _scrollRelayoutGuard;
    private float _nextDistanceRefresh;
    private readonly List<BattlefieldUnit> _orderedShips = new(256);
    private readonly List<int> _filteredShipIndices = new(256);
    private VisualElement? _filterBar;
    private VisualElement? _listHost;
    private Label? _shipGroupHeader;
    private VisualElement? _spacerTop;
    private VisualElement? _shipRowsHost;
    private VisualElement? _spacerBottom;

    public TacticalRightRail(
        VisualElement root,
        TacticalObjectCommandMenu objectCommandMenu,
        Action? refreshCombatUi = null)
    {
        _hostRoot = root;
        _objectCommandMenu = objectCommandMenu;
        _refreshCombatUi = refreshCombatUi;
        _battlefieldScroll = root.Q<ScrollView>("vision-rail-scroll");
        _objectScroll = root.Q<ScrollView>("object-overview-scroll");
        _toggleBtn = root.Q<Button>("btn-rail-mode-toggle");
        _battlefieldContent = _battlefieldScroll?.contentContainer;
        _objectContent = _objectScroll?.contentContainer;

        if (_toggleBtn != null)
        {
            _toggleBtn.clicked += () =>
            {
                TacticalSelectionState.ToggleRailMode();
                InvalidateCaches();
                ApplyModeVisibility();
                RefreshToggleLabel();
                var core = GameAppHost.Instance?.Core;
                if (core != null)
                {
                    RefreshBattlefields(core.State);
                }

                AgentSessionDebugLog.Write(
                    "H3",
                    "TacticalRightRail.toggle",
                    "mode_changed",
                    new { mode = TacticalSelectionState.RightRailMode.ToString() });
            };
        }

        TacticalSelectionState.RailModeChanged += ApplyModeVisibility;
        ApplyModeVisibility();
        RefreshToggleLabel();
    }

    // li3etocoode345

    public void Refresh(GameState state)
    {
        if (Time.unscaledTime >= _nextRailForceRefresh)
        {
            _nextRailForceRefresh = Time.unscaledTime + 10f;
            InvalidateCaches();
        }

        RefreshBattlefields(state);
        RefreshObjects(state);
        RefreshToggleLabel();
    }

    /// <summary>交互帧：仅高亮 / 节流距离；禁止追加行、禁止 Clear。</summary>
    public void RefreshInteractionOnly(GameState state)
    {
        RefreshToggleLabel();
        var bf = FindActiveBattlefield(state);
        if (bf == null)
        {
            return;
        }

        RefreshSelectionHighlight();
        if (_shipRowsHost != null
            && _shipRowsHost.childCount > 0
            && Time.unscaledTime >= _nextDistanceRefresh)
        {
            _nextDistanceRefresh = Time.unscaledTime + ObjectOverviewDistanceIntervalSec;
            UpdateObjectRowDistances(state, bf);
        }
    }

    /// <summary>重表现帧：推进虚拟窗步进填充（密舰队用）。</summary>
    public void RefreshHeavyStep(GameState state)
    {
        var bf = FindActiveBattlefield(state);
        if (bf == null)
        {
            return;
        }

        if (_overviewBuilding)
        {
            AppendOverviewShipSteps(state, bf);
        }
    }

    public void InvalidateCaches()
    {
        _lastObjectOverviewKey = "";
        _lastBattlefieldRailKey = "";
        _overviewBuilding = false;
        _shipWindowStart = 0;
        _shipRowsBuiltInWindow = 0;
        _orderedShips.Clear();
        _filteredShipIndices.Clear();
    }

    public void RefreshSelectionHighlight()
    {
        UpdateObjectRowSelection();
    }

    // liketocoode3a5

    private void ApplyModeVisibility()
    {
        var bf = TacticalSelectionState.RightRailMode == TacticalRightRailMode.Battlefield;
        if (_battlefieldScroll != null)
        {
            _battlefieldScroll.style.display = bf ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_objectScroll != null)
        {
            _objectScroll.style.display = bf ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private void RefreshToggleLabel()
    {
        if (_toggleBtn == null)
        {
            return;
        }
        _toggleBtn.text = TacticalSelectionState.RightRailMode == TacticalRightRailMode.Battlefield
            ? "切换：场景总览"
            : "切换：可观察战场";
    }

    // liketocoode34e

    private void RefreshBattlefields(GameState state)
    {
        if (_battlefieldContent == null)
        {
            AgentSessionDebugLog.Write(
                "H3",
                "TacticalRightRail.RefreshBattlefields",
                "content_null",
                new { scrollNull = _battlefieldScroll == null });
            return;
        }

        var groups = VisionLocationService.ListBattlefieldVisionGroups(state);
        var bfKey = string.Join("|", groups.Select(g =>
            g.BattlefieldId + ":" + string.Join(",", g.Characters.Select(c =>
                c.MemberId + ":" + (c.InTransit ? "t" : "f")
                + ":" + (c.CanPossess ? "p" : "") + (c.CanTacticalLink ? "v" : "")))))
            + "#" + (state.activeBattlefieldId ?? "")
            + "#" + (state.possessingMemberId ?? "")
            + "#" + (state.tacticalCameraUnitId ?? "");
        var cacheSkip = bfKey == _lastBattlefieldRailKey && _battlefieldContent.childCount > 0
            && (groups.Count > 0 || TacticalSelectionState.RightRailMode != TacticalRightRailMode.Battlefield);
        AgentSessionDebugLog.Write(
            "H3",
            "TacticalRightRail.RefreshBattlefields",
            "render",
            new
            {
                mode = TacticalSelectionState.RightRailMode.ToString(),
                contentNull = false,
                scrollNull = _battlefieldScroll == null,
                groupsCount = groups.Count,
                cacheSkip,
                childCountBefore = _battlefieldContent.childCount,
                explain = groups.Count == 0 ? VisionLocationService.ExplainEmptyDescentList(state) : "",
            });
        if (cacheSkip)
        {
            return;
        }

        _lastBattlefieldRailKey = bfKey;
        _battlefieldContent.Clear();
        if (groups.Count == 0)
        {
            _battlefieldContent.Add(MakeHint(VisionLocationService.ExplainEmptyDescentList(state)
                + "（点底部按钮可切回场景总览）"));
            return;
        }

        foreach (var group in groups)
        {
            BattlefieldState? bf = null;
            foreach (var candidate in state.battlefields)
            {
                if (group.BattlefieldId.Equals(candidate.battlefieldId, StringComparison.Ordinal))
                {
                    bf = candidate;
                    break;
                }
            }

            var header = bf != null
                ? MapLocationFormatter.FormatBattlefield(state, bf)
                : group.LocationKey;
            _battlefieldContent.Add(MakeGroupHeader(header));

            foreach (var entry in group.Characters)
            {
                _battlefieldContent.Add(BuildDescentEntryRow(state, entry, bf));
            }
        }
    }

    private VisualElement BuildDescentEntryRow(GameState state, VisionDescentEntry entry, BattlefieldState? bf)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (entry.BattlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal)
            && (entry.MemberId.Equals(state.possessingMemberId, StringComparison.Ordinal)
                || entry.UnitId != null && entry.UnitId.Equals(state.tacticalCameraUnitId, StringComparison.Ordinal)))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        var iconHost = BuildRailIconHost(ResolveMemberShipIcon(state, entry.MemberId));
        row.Add(iconHost);

        var tags = new List<string>();
        if (entry.CanPossess)
        {
            tags.Add("可附身");
        }

        if (entry.CanTacticalLink)
        {
            tags.Add("视角");
        }
        if (!entry.CanPossess && !entry.CanTacticalLink)
        {
            tags.Add("词条");
        }

        if (entry.InTransit)
        {
            tags.Add("跃迁中");
        }
        else if (entry.UnitId == null)
        {
            tags.Add("指定地点");
        }

        var labelText = entry.MemberName;
        if (tags.Count > 0)
        {
            labelText += " · " + string.Join("/", tags);
        }

        var name = new Label(labelText);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var captured = entry;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            ActivateDescentEntry(captured);
            evt.StopPropagation();
        });
        return row;
    }

    private static Texture2D? ResolveMemberShipIcon(GameState state, string memberId)
    {
        foreach (var member in state.members)
        {
            if (!memberId.Equals(member.memberId, StringComparison.Ordinal))
            {
                continue;
            }

            var hull = ShipRegistry.LoadDefault().FindHull(member.equippedHullId);
            return TacticalIconCatalog.ResolveShipIcon(hull?.tonnageClass);
        }

        return TacticalIconCatalog.ResolveShipIcon(null);
    }

    private void ActivateDescentEntry(VisionDescentEntry entry)
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }

        var state = core.State;
        BattlefieldState? targetBf = null;
        foreach (var bf in state.battlefields)
        {
            if (entry.BattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                targetBf = bf;
                break;
            }
        }

        PossessionService.SwitchBattlefield(state, entry.BattlefieldId);
        TacticalSelectionState.ClearOnBattlefieldSwitch();

        if (entry.CanPossess && !entry.InTransit && entry.UnitId != null)
        {
            PossessionService.Possess(state, entry.MemberId);
            _refreshCombatUi?.Invoke();
            return;
        }

        state.possessingMemberId = null;
        if (entry.UnitId != null)
        {
            state.tacticalCameraUnitId = entry.UnitId;
        }

        _refreshCombatUi?.Invoke();
    }

    // liketocoo3e345

    private void RefreshObjects(GameState state)
    {
        if (_objectContent == null)
        {
            return;
        }

        var bf = FindActiveBattlefield(state);
        if (bf == null)
        {
            if (_lastObjectOverviewKey != "_empty")
            {
                _objectContent.Clear();
                _filterBar = null;
                _listHost = null;
                _objectContent.Add(MakeHint("无活跃战场"));
                _lastObjectOverviewKey = "_empty";
                _overviewBuilding = false;
            }

            return;
        }

        var key = BuildObjectOverviewKey(bf, state);
        if (key != _lastObjectOverviewKey || _listHost == null)
        {
            BeginObjectOverview(state, bf, key);
        }

        if (_overviewBuilding)
        {
            AppendOverviewShipSteps(state, bf);
            return;
        }

        UpdateObjectRowSelection();
        if (Time.unscaledTime >= _nextDistanceRefresh)
        {
            _nextDistanceRefresh = Time.unscaledTime + ObjectOverviewDistanceIntervalSec;
            UpdateObjectRowDistances(state, bf);
        }
    }

    private void BeginObjectOverview(GameState state, BattlefieldState bf, string key)
    {
        _lastObjectOverviewKey = key;
        _objectContent!.Clear();
        _orderedShips.Clear();
        _filteredShipIndices.Clear();
        _shipWindowStart = 0;
        _shipRowsBuiltInWindow = 0;
        _overviewBuilding = true;
        _shipGroupHeader = null;
        _spacerTop = null;
        _shipRowsHost = null;
        _spacerBottom = null;

        RebuildFilterBar(state, bf);

        _listHost = new VisualElement { name = "object-overview-list" };
        _listHost.AddToClassList("rtcombat-rail-object-list");
        _objectContent.Add(_listHost);

        // ① 全部其他场景边缘占位（通常很少，一次建完）
        var sceneProxies = bf.units
            .Where(u => BattlefieldSceneProxyService.IsSceneProxy(u) && !u.IsDestroyed())
            .OrderBy(u => BattlefieldSceneProxyService.ResolveDistanceAu(state, bf, u))
            .ThenBy(u => u.displayName ?? u.unitId, StringComparer.Ordinal)
            .ToList();
        var offSceneLinks = sceneProxies.Count == 0
            ? BattlefieldSceneProxyService.ListOffSceneLinks(state, bf)
            : new List<TacticalOffSceneLink>();
        if (sceneProxies.Count > 0 || offSceneLinks.Count > 0)
        {
            _listHost.Add(MakeGroupHeader("其他场景"));
            foreach (var u in sceneProxies)
            {
                _listHost.Add(BuildSceneProxyRow(state, bf, u));
            }

            foreach (var link in offSceneLinks)
            {
                _listHost.Add(BuildOffSceneLinkRow(state, link));
            }
        }

        // ② 舰船按距焦距升序（含跃迁中；不按吨位分组）
        var shipScratch = new List<(BattlefieldUnit u, float dist)>(bf.units.Count);
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var warp = VisionGate.IsWarpInbound(u, bf.timeSec);
            var dist = warp ? ResolveDistanceToFocusM(state, bf, u) : ResolveDistanceToFocusM(state, bf, u);
            if (dist < 0f)
            {
                dist = float.MaxValue * 0.5f;
            }

            shipScratch.Add((u, dist));
        }

        if (bf.battlefieldId != null)
        {
            foreach (var entry in state.tacticalWarpInTransit)
            {
                if (entry.unit == null || entry.unit.IsDestroyed()
                    || !bf.battlefieldId.Equals(entry.toBattlefieldId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (entry.unit.side != UnitSide.FRIENDLY)
                {
                    continue;
                }

                var already = false;
                foreach (var s in shipScratch)
                {
                    if (s.u.unitId != null && s.u.unitId.Equals(entry.unit.unitId, StringComparison.Ordinal))
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                {
                    shipScratch.Add((entry.unit, float.MaxValue * 0.25f));
                }
            }
        }

        static int CompareShip(
            (BattlefieldUnit u, float dist) a,
            (BattlefieldUnit u, float dist) b)
        {
            var c = a.dist.CompareTo(b.dist);
            if (c != 0)
            {
                return c;
            }

            return string.Compare(a.u.displayName ?? a.u.unitId, b.u.displayName ?? b.u.unitId,
                StringComparison.Ordinal);
        }

        try
        {
            var entriesById = shipScratch
                .Where(entry => entry.u.unitId != null)
                .ToDictionary(entry => entry.u.unitId!, StringComparer.Ordinal);
            var childrenByParent = shipScratch
                .Where(entry => entry.u.parentUnitId != null
                                && entriesById.ContainsKey(entry.u.parentUnitId))
                .GroupBy(entry => entry.u.parentUnitId!, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(entry => entry, Comparer<(BattlefieldUnit u, float dist)>.Create(CompareShip))
                        .ToList(),
                    StringComparer.Ordinal);
            var roots = shipScratch
                .Where(entry => entry.u.parentUnitId == null
                                || !entriesById.ContainsKey(entry.u.parentUnitId))
                .OrderBy(entry => entry, Comparer<(BattlefieldUnit u, float dist)>.Create(CompareShip))
                .ToList();
            var appended = new HashSet<string>(StringComparer.Ordinal);
            void AppendTree((BattlefieldUnit u, float dist) entry)
            {
                if (entry.u.unitId == null || !appended.Add(entry.u.unitId))
                {
                    return;
                }
                _orderedShips.Add(entry.u);
                if (childrenByParent.TryGetValue(entry.u.unitId, out var children))
                {
                    foreach (var child in children)
                    {
                        AppendTree(child);
                    }
                }
            }
            foreach (var root in roots)
            {
                AppendTree(root);
            }
            foreach (var entry in shipScratch.OrderBy(entry => entry, Comparer<(BattlefieldUnit u, float dist)>.Create(CompareShip)))
            {
                AppendTree(entry);
            }

            RebuildFilteredShipIndices();
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "F2-F3",
                "TacticalRightRail.cs:BeginObjectOverview",
                "object overview rebuilt",
                new
                {
                    ordered = _orderedShips.Count,
                    filtered = _filteredShipIndices.Count,
                    tonnageFilter = _tonnageFilter,
                    scratch = shipScratch.Count,
                    roots = roots.Count,
                });
            // #endregion
        }
        catch (Exception ex)
        {
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "F2",
                "TacticalRightRail.cs:BeginObjectOverview:exception",
                "object overview rebuild failed",
                new
                {
                    error = ex.GetType().Name,
                    message = ex.Message,
                    tonnageFilter = _tonnageFilter,
                    scratch = shipScratch.Count,
                    ordered = _orderedShips.Count,
                });
            // #endregion
            throw;
        }

        _shipWindowStart = 0;
        _shipRowsBuiltInWindow = 0;
        EnsureVirtualShipStructure();
        UpdateShipGroupHeader();
        UpdateVirtualSpacers();
        HookObjectScroll();
        AppendOverviewShipSteps(state, bf);
    }

    private void RebuildFilteredShipIndices()
    {
        _filteredShipIndices.Clear();
        var visibleIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, BattlefieldUnit> byId;
        try
        {
            byId = _orderedShips
                .Where(unit => unit.unitId != null)
                .ToDictionary(unit => unit.unitId!, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "F2",
                "TacticalRightRail.cs:RebuildFilteredShipIndices:byId",
                "filter index dict failed",
                new
                {
                    error = ex.GetType().Name,
                    message = ex.Message,
                    ordered = _orderedShips.Count,
                    tonnageFilter = _tonnageFilter,
                });
            // #endregion
            throw;
        }

        foreach (var unit in _orderedShips)
        {
            if (_tonnageFilter != null
                && string.Equals(NormalizeFilterTonnage(unit), _tonnageFilter, StringComparison.Ordinal)
                && unit.unitId != null)
            {
                visibleIds.Add(unit.unitId);
                var parentId = unit.parentUnitId;
                while (parentId != null && byId.TryGetValue(parentId, out var parent))
                {
                    if (!visibleIds.Add(parentId))
                    {
                        break;
                    }
                    parentId = parent.parentUnitId;
                }
            }
        }
        for (var i = 0; i < _orderedShips.Count; i++)
        {
            var u = _orderedShips[i];
            if (_tonnageFilter == null
                || u.unitId != null && visibleIds.Contains(u.unitId))
            {
                _filteredShipIndices.Add(i);
            }
        }
    }

    private void EnsureVirtualShipStructure()
    {
        if (_listHost == null)
        {
            return;
        }

        _shipGroupHeader = MakeGroupHeader("舰船");
        _listHost.Add(_shipGroupHeader);

        _spacerTop = new VisualElement { name = "object-overview-spacer-top" };
        _spacerTop.pickingMode = PickingMode.Ignore;
        _listHost.Add(_spacerTop);

        _shipRowsHost = new VisualElement { name = "object-overview-ship-rows" };
        _shipRowsHost.AddToClassList("rtcombat-rail-object-list");
        _listHost.Add(_shipRowsHost);

        _spacerBottom = new VisualElement { name = "object-overview-spacer-bottom" };
        _spacerBottom.pickingMode = PickingMode.Ignore;
        _listHost.Add(_spacerBottom);
    }

    private void UpdateShipGroupHeader()
    {
        if (_shipGroupHeader == null)
        {
            return;
        }

        var n = _filteredShipIndices.Count;
        _shipGroupHeader.text = _tonnageFilter == null
            ? $"舰船（按距离 · {n}）"
            : $"舰船（{TacticalIconCatalog.GroupLabel(_tonnageFilter)} · {n}）";
    }

    private void UpdateVirtualSpacers()
    {
        if (_spacerTop == null || _spacerBottom == null)
        {
            return;
        }

        var total = _filteredShipIndices.Count;
        var visible = Math.Min(ObjectOverviewVisibleShipRows, Math.Max(0, total - _shipWindowStart));
        _spacerTop.style.height = _shipWindowStart * ObjectOverviewRowHeightPx;
        _spacerBottom.style.height = Math.Max(0, total - _shipWindowStart - visible)
            * ObjectOverviewRowHeightPx;
    }

    private void HookObjectScroll()
    {
        if (_scrollHooked || _objectScroll == null)
        {
            return;
        }

        _objectScroll.verticalScroller.valueChanged += OnObjectScrollChanged;
        _scrollHooked = true;
    }

    private void OnObjectScrollChanged(float value)
    {
        if (_scrollRelayoutGuard || _filteredShipIndices.Count <= ObjectOverviewVisibleShipRows)
        {
            return;
        }

        var maxStart = Math.Max(0, _filteredShipIndices.Count - ObjectOverviewVisibleShipRows);
        var start = Mathf.Clamp(
            Mathf.FloorToInt(value / ObjectOverviewRowHeightPx),
            0,
            maxStart);
        if (start == _shipWindowStart)
        {
            return;
        }

        var core = GameAppHost.Instance?.Core;
        var state = core?.State;
        var bf = state != null ? FindActiveBattlefield(state) : null;
        if (state == null || bf == null)
        {
            return;
        }

        _scrollRelayoutGuard = true;
        try
        {
            _shipWindowStart = start;
            _shipRowsBuiltInWindow = 0;
            _overviewBuilding = true;
            _shipRowsHost?.Clear();
            UpdateVirtualSpacers();
            AppendOverviewShipSteps(state, bf, forceFillWindow: true);
        }
        finally
        {
            _scrollRelayoutGuard = false;
        }
    }

    private void AppendOverviewShipSteps(
        GameState state,
        BattlefieldState bf,
        bool forceFillWindow = false)
    {
        if (_shipRowsHost == null)
        {
            _overviewBuilding = false;
            return;
        }

        var targetVisible = Math.Min(
            ObjectOverviewVisibleShipRows,
            Math.Max(0, _filteredShipIndices.Count - _shipWindowStart));
        var budget = forceFillWindow ? targetVisible : ObjectOverviewStepRows;
        var added = 0;
        while (_shipRowsBuiltInWindow < targetVisible && added < budget)
        {
            var filteredIdx = _shipWindowStart + _shipRowsBuiltInWindow;
            if (filteredIdx < 0 || filteredIdx >= _filteredShipIndices.Count)
            {
                break;
            }

            var u = _orderedShips[_filteredShipIndices[filteredIdx]];
            var warp = VisionGate.IsWarpInbound(u, bf.timeSec);
            _shipRowsHost.Add(BuildObjectRow(
                state,
                u,
                bf,
                indent: u.parentUnitId != null,
                warp: warp));
            _shipRowsBuiltInWindow++;
            added++;
        }

        UpdateVirtualSpacers();
        if (_shipRowsBuiltInWindow >= targetVisible)
        {
            _overviewBuilding = false;
            UpdateObjectRowSelection();
        }
    }

    private void RebuildFilterBar(GameState state, BattlefieldState bf)
    {
        _filterBar = new VisualElement { name = "object-overview-filter" };
        _filterBar.AddToClassList("rtcombat-rail-filter-bar");

        var allBtn = new Button(() => SetTonnageFilter(null)) { text = "全部" };
        allBtn.AddToClassList("rtcombat-rail-filter-btn");
        allBtn.AddToClassList("rtcombat-rail-filter-all");
        if (_tonnageFilter == null)
        {
            allBtn.AddToClassList("rtcombat-rail-filter-btn-active");
        }

        _filterBar.Add(allBtn);

        var tonnages = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            tonnages.Add(NormalizeFilterTonnage(u));
        }

        foreach (var tc in tonnages)
        {
            var tonnage = tc;
            var btn = new Button(() => SetTonnageFilter(tonnage));
            btn.userData = tonnage;
            btn.AddToClassList("rtcombat-rail-filter-btn");
            btn.tooltip = TacticalIconCatalog.GroupLabel(tonnage);
            if (string.Equals(_tonnageFilter, tonnage, StringComparison.Ordinal))
            {
                btn.AddToClassList("rtcombat-rail-filter-btn-active");
            }

            var tex = TacticalIconCatalog.ResolveShipIcon(tonnage);
            if (tex != null)
            {
                btn.style.backgroundImage = new StyleBackground(tex);
                btn.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                btn.text = "";
            }
            else
            {
                btn.text = tonnage.Length <= 3 ? tonnage : tonnage[..2];
            }

            _filterBar.Add(btn);
        }

        _objectContent!.Add(_filterBar);
        // #region agent log
        AgentSessionDebugLog.WriteDebugSession(
            "F5",
            "TacticalRightRail.cs:RebuildFilterBar",
            "filter bar rebuilt",
            new
            {
                buttonCount = _filterBar.childCount,
                tonnages = tonnages.ToArray(),
                activeFilter = _tonnageFilter,
                railMode = TacticalSelectionState.RightRailMode.ToString(),
            });
        // #endregion
    }

    private void SetTonnageFilter(string? tonnage)
    {
        var previous = _tonnageFilter;
        if (tonnage != null && string.Equals(_tonnageFilter, tonnage, StringComparison.Ordinal))
        {
            _tonnageFilter = null;
        }
        else
        {
            _tonnageFilter = tonnage;
        }

        var core = GameAppHost.Instance?.Core;
        var state = core?.State;
        var bf = state != null ? FindActiveBattlefield(state) : null;
        if (state == null || bf == null || _listHost == null)
        {
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "F1",
                "TacticalRightRail.cs:SetTonnageFilter:early-return",
                "filter click aborted",
                new
                {
                    requested = tonnage,
                    previous,
                    applied = _tonnageFilter,
                    hasState = state != null,
                    hasBf = bf != null,
                    hasListHost = _listHost != null,
                    hasRowsHost = _shipRowsHost != null,
                    ordered = _orderedShips.Count,
                    railMode = TacticalSelectionState.RightRailMode.ToString(),
                });
            // #endregion
            return;
        }

        RebuildFilteredShipIndices();
        _shipWindowStart = 0;
        _shipRowsBuiltInWindow = 0;
        _shipRowsHost?.Clear();
        UpdateShipGroupHeader();
        UpdateVirtualSpacers();
        _overviewBuilding = true;
        RefreshFilterBarActiveStyles();
        AppendOverviewShipSteps(state, bf, forceFillWindow: true);
        // #region agent log
        AgentSessionDebugLog.WriteDebugSession(
            "F1-F4",
            "TacticalRightRail.cs:SetTonnageFilter",
            "filter applied",
            new
            {
                requested = tonnage,
                previous,
                applied = _tonnageFilter,
                ordered = _orderedShips.Count,
                filtered = _filteredShipIndices.Count,
                rowsBuilt = _shipRowsBuiltInWindow,
                rowsHostChildren = _shipRowsHost?.childCount ?? -1,
                overviewBuilding = _overviewBuilding,
                header = _shipGroupHeader?.text,
            });
        // #endregion
    }

    private void RefreshFilterBarActiveStyles()
    {
        if (_filterBar == null)
        {
            return;
        }

        foreach (var child in _filterBar.Children())
        {
            if (child is not Button btn)
            {
                continue;
            }

            btn.RemoveFromClassList("rtcombat-rail-filter-btn-active");
            if (_tonnageFilter == null && btn.ClassListContains("rtcombat-rail-filter-all"))
            {
                btn.AddToClassList("rtcombat-rail-filter-btn-active");
            }
            else if (_tonnageFilter != null
                     && btn.userData is string tc
                     && string.Equals(tc, _tonnageFilter, StringComparison.Ordinal))
            {
                btn.AddToClassList("rtcombat-rail-filter-btn-active");
            }
        }
    }

    private static string NormalizeFilterTonnage(BattlefieldUnit u)
    {
        if (JumpBridgeUnitService.IsJumpBridgeBuilding(u))
        {
            return JumpBridgeUnitService.TonnageClass;
        }

        if (u.isBuilding)
        {
            return "BUILDING";
        }

        return u.tonnageClass ?? "UNKNOWN";
    }

    // liketoco0de345

    private VisualElement BuildObjectRow(GameState state, BattlefieldUnit u, BattlefieldState bf, bool indent, bool warp)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        if (indent)
        {
            row.AddToClassList("rtcombat-rail-object-indent");
        }
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (u.unitId != null && u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        // 舰载机/无人机：子单位缩进，便于与母舰区分
        if (!string.IsNullOrEmpty(u.parentUnitId) || IsDedicatedWing(u.tonnageClass))
        {
            row.AddToClassList("rtcombat-rail-object-indent");
        }

        var iconHost = BuildRailIconHost(ResolveRowIcon(u));
        row.Add(iconHost);

        if (!u.isBuilding)
        {
            AddSideBadge(iconHost, u.side);
        }

        var uid = u.unitId;
        var ships = ShipRegistry.LoadDefault();
        var wingLine = WingRowLabel(u, bf);
        var line = wingLine != null
            ? FormatWingOverviewLine(
                u,
                wingLine,
                warp,
                warp ? -1f : ResolveDistanceToFocusM(state, bf, u))
            : DisplayLabels.ObjectOverviewCompact(
                state,
                u,
                ships,
                warp ? -1f : ResolveDistanceToFocusM(state, bf, u),
                warp);
        var name = new Label(line);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        row.userData = uid;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            TacticalSelectionState.SetSelectedTarget(uid);
            if (evt.shiftKey && u.side == UnitSide.FRIENDLY && uid != null && !u.isBuilding)
            {
                TacticalSelectionState.SetBoxSelection(new[] { uid }, additive: true);
            }
            else
            {
                var iconRect = iconHost.worldBound;
                var world = new Vector2(iconRect.x + iconRect.width * 0.5f, iconRect.y + iconRect.height * 0.5f);
                _objectCommandMenu.ShowAtWorld(world, uid);
            }

            evt.StopPropagation();
        });
        return row;
    }

    private static float ResolveDistanceToFocusM(GameState state, BattlefieldState bf, BattlefieldUnit u)
    {
        if (VisionGate.IsWarpInbound(u, bf.timeSec)
            || u.warpPhase == TacticalWarpPhase.InTransit)
        {
            return -1f;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        if (focus == null)
        {
            return -1f;
        }

        if (ReferenceEquals(focus, u))
        {
            return 0f;
        }

        var dx = u.x - focus.x;
        var dy = u.y - focus.y;
        var dz = u.z - focus.z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void UpdateObjectRowDistances(GameState state, BattlefieldState bf)
    {
        // 仅刷新虚拟窗内舰船行（≤ Visible）；其他场景占位改文案成本低，并入同循环
        var host = _shipRowsHost ?? _listHost ?? _objectContent;
        if (host == null)
        {
            return;
        }

        var ships = ShipRegistry.LoadDefault();
        foreach (var child in host.Children())
        {
            if (child.userData is not string unitId)
            {
                continue;
            }

            var unit = FindOverviewUnit(state, bf, unitId);
            if (unit == null)
            {
                continue;
            }

            foreach (var label in child.Children())
            {
                if (label is not Label nameLabel || !nameLabel.ClassListContains("rtcombat-rail-name"))
                {
                    continue;
                }

                if (BattlefieldSceneProxyService.IsSceneProxy(unit))
                {
                    var au = BattlefieldSceneProxyService.ResolveDistanceAu(state, bf, unit);
                    nameLabel.text = BattlefieldSceneProxyService.FormatSceneProxyLabel(
                        unit.displayName ?? unit.unitId ?? "其他场景",
                        au);
                    continue;
                }

                var inTransit = VisionGate.IsWarpInbound(unit, bf.timeSec)
                    || unit.warpPhase == TacticalWarpPhase.InTransit;
                var wingLine = WingRowLabel(unit, bf);
                nameLabel.text = wingLine != null
                    ? FormatWingOverviewLine(
                        unit,
                        wingLine,
                        inTransit,
                        inTransit ? -1f : ResolveDistanceToFocusM(state, bf, unit))
                    : DisplayLabels.ObjectOverviewCompact(
                        state,
                        unit,
                        ships,
                        inTransit ? -1f : ResolveDistanceToFocusM(state, bf, unit),
                        inTransit);
            }
        }
    }

    private static string FormatWingOverviewLine(
        BattlefieldUnit u,
        string wingLine,
        bool warp,
        float distanceM)
    {
        var kind = "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
            ? "舰载机"
            : "DRONE".Equals(u.tonnageClass, StringComparison.Ordinal)
                || "SHUTTLE".Equals(u.tonnageClass, StringComparison.Ordinal)
                ? "无人机"
                : TacticalIconCatalog.GroupLabel(u.tonnageClass ?? "UNKNOWN");
        if (warp)
        {
            return kind + " · " + wingLine + " · 跃迁在途";
        }

        return kind + " · " + wingLine + " · " + DisplayLabels.ObjectOverviewDistanceLine(distanceM);
    }

    private static BattlefieldUnit? FindOverviewUnit(GameState state, BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        foreach (var entry in state.tacticalWarpInTransit)
        {
            if (unitId.Equals(entry.unit.unitId, StringComparison.Ordinal))
            {
                return entry.unit;
            }
        }

        return null;
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    private static VisualElement BuildRailIconHost(Texture2D? tex)
    {
        var host = new VisualElement();
        host.AddToClassList("rtcombat-rail-icon");
        host.pickingMode = PickingMode.Ignore;
        host.style.overflow = Overflow.Hidden;
        if (tex != null)
        {
            var img = new Image
            {
                image = tex,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            img.style.width = Length.Percent(100);
            img.style.height = Length.Percent(100);
            img.style.alignSelf = Align.Center;
            host.Add(img);
        }

        return host;
    }

    private static void AddSideBadge(VisualElement iconHost, UnitSide side, string? textOverride = null)
    {
        var badge = new Label(textOverride ?? (side == UnitSide.ENEMY ? "−" : "+"));
        badge.pickingMode = PickingMode.Ignore;
        badge.AddToClassList(side == UnitSide.ENEMY ? "rtcombat-rail-badge-hostile" : "rtcombat-rail-badge-friendly");
        iconHost.Add(badge);
    }

    private static Texture2D? ResolveRowIcon(BattlefieldUnit u)
    {
        if (BattlefieldSceneProxyService.IsSceneProxy(u))
        {
            return TacticalIconCatalog.ResolveSceneProxyIcon(u.sceneProxyTargetKind);
        }

        if (JumpBridgeUnitService.IsJumpBridgeBuilding(u))
        {
            return TacticalIconCatalog.ResolveEventRegionIcon(EventRegionKinds.JumpBridge);
        }

        var hull = u.hullId != null ? ShipRegistry.LoadDefault().FindHull(u.hullId) : null;
        return TacticalIconCatalog.ResolveUnitShipIcon(u, hull);
    }

    private VisualElement BuildSceneProxyRow(GameState state, BattlefieldState bf, BattlefieldUnit u)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (u.unitId != null && u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        var iconHost = BuildRailIconHost(TacticalIconCatalog.ResolveSceneProxyIcon(u.sceneProxyTargetKind));
        AddSideBadge(iconHost, UnitSide.FRIENDLY, "⤴");
        row.Add(iconHost);

        var au = BattlefieldSceneProxyService.ResolveDistanceAu(state, bf, u);
        var name = new Label(BattlefieldSceneProxyService.FormatSceneProxyLabel(
            u.displayName ?? u.unitId ?? "其他场景",
            au));
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var uid = u.unitId;
        row.userData = uid;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            TacticalSelectionState.SetSelectedTarget(uid);
            var iconRect = iconHost.worldBound;
            var world = new Vector2(iconRect.x + iconRect.width * 0.5f, iconRect.y + iconRect.height * 0.5f);
            _objectCommandMenu.ShowAtWorld(world, uid);
            evt.StopPropagation();
        });
        return row;
    }

    private VisualElement BuildOffSceneLinkRow(GameState state, TacticalOffSceneLink link)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (link.UnitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        var iconHost = BuildRailIconHost(TacticalIconCatalog.ResolveSceneProxyIcon(link.Kind));
        AddSideBadge(iconHost, UnitSide.FRIENDLY, "⤴");
        row.Add(iconHost);

        var name = new Label(BattlefieldSceneProxyService.FormatSceneProxyLabel(link.DisplayName, link.DistanceAu));
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var uid = link.UnitId;
        row.userData = uid;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            TacticalSelectionState.SetSelectedTarget(uid);
            var iconRect = iconHost.worldBound;
            var world = new Vector2(iconRect.x + iconRect.width * 0.5f, iconRect.y + iconRect.height * 0.5f);
            _objectCommandMenu.ShowAtWorld(world, uid);
            evt.StopPropagation();
        });
        return row;
    }

    private static string BuildObjectOverviewKey(BattlefieldState bf, GameState state)
    {
        // 粗粒度：场上艘数变化才重启步进；不含选中 / 过滤器（过滤只重建舰船段）
        var alive = 0;
        var dead = 0;
        var proxies = 0;
        foreach (var u in bf.units)
        {
            if (BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                if (!u.IsDestroyed())
                {
                    proxies++;
                }

                continue;
            }

            if (u.IsDestroyed())
            {
                dead++;
            }
            else
            {
                alive++;
            }
        }

        var links = BattlefieldSceneProxyService.ListOffSceneLinks(state, bf).Count;
        var transit = 0;
        foreach (var e in state.tacticalWarpInTransit)
        {
            if (bf.battlefieldId != null
                && bf.battlefieldId.Equals(e.toBattlefieldId, StringComparison.Ordinal))
            {
                transit++;
            }
        }

        // 含舰载机/无人机计数：放出后 alive 若被其它死亡对消也可能漏刷，故单独记账
        var strike = 0;
        var drones = 0;
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed())
            {
                continue;
            }

            if ("STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal))
            {
                strike++;
            }
            else if ("DRONE".Equals(u.tonnageClass, StringComparison.Ordinal)
                     || "SHUTTLE".Equals(u.tonnageClass, StringComparison.Ordinal))
            {
                drones++;
            }
        }

        return (bf.battlefieldId ?? "?") + "|a" + alive + "|d" + dead + "|p" + proxies
            + "|l" + links + "|t" + transit + "|n" + bf.units.Count
            + "|s" + strike + "|r" + drones;
    }

    private void UpdateObjectRowSelection()
    {
        var selected = TacticalSelectionState.SelectedTargetUnitId;
        void Paint(VisualElement? host)
        {
            if (host == null)
            {
                return;
            }

            foreach (var child in host.Children())
            {
                if (child.userData is not string uid)
                {
                    continue;
                }

                if (uid.Equals(selected, StringComparison.Ordinal))
                {
                    child.AddToClassList("rtcombat-rail-item-active");
                }
                else
                {
                    child.RemoveFromClassList("rtcombat-rail-item-active");
                }
            }
        }

        Paint(_shipRowsHost);
        Paint(_listHost);
    }

    // lik3tocoode345

    private static bool IsDedicatedWing(string? tonnageClass) =>
        "STRIKE_CRAFT".Equals(tonnageClass, StringComparison.Ordinal)
        || "DRONE".Equals(tonnageClass, StringComparison.Ordinal)
        || "SHUTTLE".Equals(tonnageClass, StringComparison.Ordinal)
        || "BOARD_SUMMON_WING".Equals(tonnageClass, StringComparison.Ordinal)
        || "MISSILE".Equals(tonnageClass, StringComparison.Ordinal);

    // lik3tocoode345

    private static string? WingRowLabel(BattlefieldUnit u, BattlefieldState bf)
    {
        if (u.parentUnitId == null && !IsDedicatedWing(u.tonnageClass))
        {
            return null;
        }
        var name = u.displayName ?? u.unitId ?? "?";
        var owner = FormatOwnerAttribution(bf, u);
        return string.IsNullOrEmpty(owner) ? name : name + " · " + owner;
    }

    private static string FormatOwnerAttribution(BattlefieldState bf, BattlefieldUnit u)
    {
        if (u.parentUnitId == null)
        {
            return u.memberId != null ? "团员 " + u.memberId : "";
        }
        var byId = bf.units
            .Where(unit => unit.unitId != null)
            .ToDictionary(unit => unit.unitId!, StringComparer.Ordinal);
        var chain = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var parentId = u.parentUnitId;
        string? rootMemberId = null;
        while (parentId != null && seen.Add(parentId))
        {
            if (!byId.TryGetValue(parentId, out var parent))
            {
                chain.Add(parentId);
                break;
            }
            chain.Add(parent.displayName ?? parent.unitId ?? parentId);
            rootMemberId = parent.memberId ?? rootMemberId;
            parentId = parent.parentUnitId;
        }
        chain.Reverse();
        return "归属 " + string.Join(" › ", chain)
               + (rootMemberId != null ? " · " + rootMemberId : "");
    }

    private static BattlefieldState? FindActiveBattlefield(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return null;
        }
        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, System.StringComparison.Ordinal))
            {
                return bf;
            }
        }
        return null;
    }

    // liketocoode3e5

    private static Label MakeGroupHeader(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-rail-group");
        return l;
    }

    // liket0coode345

    private static Label MakeHint(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-subtitle");
        return l;
    }
}
