using System;
using System.Collections.Generic;
using TopDog.Lobby;
using TopDog.Net.Lan;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md §加入他人游戏
 * 本文件: JoinLanController.cs — LAN 房间浏览/加入 UI
 * 【机制要点】
 * · LanRoomBrowser → 加入大厅
 * 【关联】CustomLobbyController · UiNavigator · MainMenuController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
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
    private string? _selectedRoomKind;
    private float _refreshTimer;

    protected override void OnDisable()
    {
        ClearDynamicHandlers();
        // li3etocoode345
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
    // liketocoode3a5
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
        // liketocoode34e
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

        // liketocoo3e345
        if (!preserveSelection)
        {
            _selectedHostIp = null;
            _selectedMapId = null;
        }

        if (_rooms.Count == 0)
        {
            SetStatus("未发现房间 · 请确认房主已开「自定义战役」或「军团约战」并在同一局域网");
        }
        else
        {
            SetStatus("发现 " + _rooms.Count + " 个房间 · 点击选择后按「加入」");
        }

        RenderRoomList();
    }

    // liketoco0de345
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
            var empty = new Label { text = "暂无房间，请确认房主已开「自定义战役」并在同一局域网（军团约战请从世界线直接进入）" };
            empty.AddToClassList("join-lan-room-empty");
            _roomList.Add(empty);
            return;
        }

        foreach (var room in _rooms)
        {
            var hostIp = room.hostIp ?? "?";
            var mapLabel = string.IsNullOrWhiteSpace(room.mapId) ? "未知地图" : room.mapId;
            var kind = string.IsNullOrWhiteSpace(room.roomKind) ? "CUSTOM" : room.roomKind;
            var ver = string.IsNullOrWhiteSpace(room.contentVersion) ? "?" : room.contentVersion;
            var versionOk = ContentVersionGate.Matches(room.contentVersion);
            var text = (room.hostName ?? hostIp) + "  ·  " + hostIp + "  ·  "
                       + room.playerCount + " 人  ·  " + kind + "  ·  " + mapLabel
                       + "  ·  " + ver
                       + (versionOk ? "" : "  ·  版号不合");
            var btn = new Button { text = text };
            btn.AddToClassList("join-lan-room-btn");
            if (!versionOk)
            {
                btn.SetEnabled(false);
            }
            if (hostIp == _selectedHostIp)
            {
                btn.AddToClassList("join-lan-room-btn-selected");
            }
            var ip = hostIp;
            var mapId = room.mapId;
            var roomKind = room.roomKind;
            var roomVer = room.contentVersion;
            EventCallback<ClickEvent> handler = _ =>
            {
                if (!ContentVersionGate.Matches(roomVer))
                {
                    SetStatus("主机版本 " + (roomVer ?? "?") + " / 本机 " + ContentVersionGate.Current + "，需一致才能联机");
                    return;
                }

                _selectedHostIp = ip;
                _selectedMapId = mapId;
                _selectedRoomKind = roomKind;
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

        PeerAnnouncement? selected = null;
        foreach (var room in _rooms)
        {
            if (room.hostIp == _selectedHostIp)
            {
                selected = room;
                break;
            }
        }

        if (selected != null && !ContentVersionGate.Matches(selected.contentVersion))
        {
            SetStatus("主机版本 " + (selected.contentVersion ?? "?")
                      + " / 本机 " + ContentVersionGate.Current + "，需一致才能联机");
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
// liketocoode3a5
}
