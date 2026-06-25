using System;
using System.Collections.Generic;
using TopDog.Lobby;
using TopDog.Net.Lan;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>UDP LAN room browser; join selected host's custom lobby.</summary>
public sealed class JoinLanController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.JoinLan;

    protected override bool UseSafeAreaInsets => false;

    private readonly List<(Button btn, EventCallback<ClickEvent> handler)> _dynamicHandlers = new();
    private readonly List<PeerAnnouncement> _rooms = new();

    private LanRoomBrowser? _browser;
    private ScrollView? _roomList;
    private Label? _statusLabel;
    private string? _selectedHostIp;
    private string? _selectedMapId;
    private float _refreshTimer;

    protected override void OnDisable()
    {
        ClearDynamicHandlers();
        _browser?.Dispose();
        _browser = null;
        base.OnDisable();
    }

    private void Update()
    {
        if (!isActiveAndEnabled || _browser == null)
        {
            return;
        }
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= 2f)
        {
            _refreshTimer = 0f;
            RebuildRoomList(preserveSelection: true);
        }
    }

    protected override void Bind(VisualElement root)
    {
        _roomList = root.Q<ScrollView>("room-list");
        _statusLabel = root.Q<Label>("lbl-status");
        _selectedHostIp = null;
        _selectedMapId = null;
        _refreshTimer = 0f;

        try
        {
            _browser?.Dispose();
            _browser = new LanRoomBrowser();
            _browser.Start();
            RebuildRoomList(preserveSelection: false);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            SetStatus("局域网浏览失败: " + e.Message);
        }

        OnClick(root, "btn-refresh", () => RebuildRoomList(preserveSelection: true));
        OnClick(root, "btn-join", JoinSelected);
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowWorldline());
    }

    private void RebuildRoomList(bool preserveSelection)
    {
        _browser?.PruneStale();
        _rooms.Clear();
        if (_browser != null)
        {
            _rooms.AddRange(_browser.ActiveRooms());
        }

        if (!preserveSelection)
        {
            _selectedHostIp = null;
            _selectedMapId = null;
        }

        if (_rooms.Count == 0)
        {
            SetStatus("未发现房间 · 请确认房主已开「自定义战役」并在同一局域网");
        }
        else
        {
            SetStatus("发现 " + _rooms.Count + " 个房间 · 点击选择后按「加入」");
        }

        RenderRoomList();
    }

    private void RenderRoomList()
    {
        if (_roomList == null)
        {
            return;
        }
        ClearDynamicHandlers();
        _roomList.Clear();

        if (_rooms.Count == 0)
        {
            var empty = new Label { text = "暂无房间，请确认房主已开「自定义战役」并在同一局域网" };
            empty.AddToClassList("join-lan-room-empty");
            _roomList.Add(empty);
            return;
        }

        foreach (var room in _rooms)
        {
            var hostIp = room.hostIp ?? "?";
            var mapLabel = string.IsNullOrWhiteSpace(room.mapId) ? "未知地图" : room.mapId;
            var text = (room.hostName ?? hostIp) + "  ·  " + hostIp + "  ·  "
                       + room.playerCount + " 人  ·  " + mapLabel;
            var btn = new Button { text = text };
            btn.AddToClassList("join-lan-room-btn");
            if (hostIp == _selectedHostIp)
            {
                btn.AddToClassList("join-lan-room-btn-selected");
            }
            var ip = hostIp;
            var mapId = room.mapId;
            EventCallback<ClickEvent> handler = _ =>
            {
                _selectedHostIp = ip;
                _selectedMapId = mapId;
                RenderRoomList();
            };
            btn.RegisterCallback(handler);
            _dynamicHandlers.Add((btn, handler));
            _roomList.Add(btn);
        }
    }

    private void JoinSelected()
    {
        if (string.IsNullOrWhiteSpace(_selectedHostIp))
        {
            SetStatus("请先选择一个房间");
            return;
        }
        GetComponent<UiNavigator>()?.ShowCustomLobbyJoin(_selectedHostIp, _selectedMapId);
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }

    private void ClearDynamicHandlers()
    {
        foreach (var (btn, handler) in _dynamicHandlers)
        {
            btn?.UnregisterCallback(handler);
        }
        _dynamicHandlers.Clear();
    }
}
