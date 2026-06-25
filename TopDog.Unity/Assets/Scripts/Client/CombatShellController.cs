using TopDog.Sim.Combat;
using TopDog.Sim.Building;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>COMBAT_PREP and AUTO combat scene UI.</summary>
public sealed class CombatShellController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.CombatShell;

    protected override bool UseSafeAreaInsets => false;

    private Label _phaseLabel;
    private Label _queueLabel;
    private Label _bodyLabel;
    private Label _statusLabel;
    private Button _autoBtn;
    private Button _realtimeBtn;
    private Button _engageBtn;
    private Button _retreatBtn;
    private float _nextRefresh;
    private float _spectatorCombatTick;
    private EventCallback<KeyDownEvent>? _keyHandler;

    protected override void OnDisable()
    {
        if (Root != null && _keyHandler != null)
        {
            Root.UnregisterCallback(_keyHandler);
        }
        _keyHandler = null;
        base.OnDisable();
    }

    protected override void Bind(VisualElement root)
    {
        _phaseLabel = root.Q<Label>("lbl-phase");
        _queueLabel = root.Q<Label>("lbl-queue");
        _bodyLabel = root.Q<Label>("lbl-body");
        _statusLabel = root.Q<Label>("lbl-status");
        _autoBtn = root.Q<Button>("btn-auto");
        _realtimeBtn = root.Q<Button>("btn-realtime");
        _engageBtn = root.Q<Button>("btn-engage");
        _retreatBtn = root.Q<Button>("btn-retreat");

        OnClick(root, "btn-auto", () => RunCommand("交战 自动"));
        OnClick(root, "btn-realtime", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core == null)
            {
                return;
            }
            SetStatus(core.CombatChooseRealtime());
            GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
        });
        OnClick(root, "btn-engage", () => RunCommand("交战 接战"));
        OnClick(root, "btn-retreat", () => RunCommand("交战 撤退"));
        OnClick(root, "btn-back-ops", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core != null)
            {
                core.SetPhase(GamePhase.OPERATIONS);
            }
        });
        BindKeyboard(root);
        RefreshAll();
    }

    private void BindKeyboard(VisualElement root)
    {
        root.focusable = true;
        _keyHandler = evt =>
        {
            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }
            MatchPauseOverlay.TryHandleEscape(root);
            evt.StopPropagation();
        };
        root.RegisterCallback(_keyHandler);
    }

    private void Update()
    {
        if (!isActiveAndEnabled || Time.unscaledTime < _nextRefresh)
        {
            return;
        }
        _nextRefresh = Time.unscaledTime + 0.25f;
        RefreshAll();
        TrySpectatorAutoCombat();
    }

    private void TrySpectatorAutoCombat()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null || !core.State.spectatorMode)
        {
            return;
        }
        if (Time.unscaledTime < _spectatorCombatTick)
        {
            return;
        }
        _spectatorCombatTick = Time.unscaledTime + 0.6f;
        var s = core.State;
        if (s.phase != GamePhase.COMBAT_PREP)
        {
            return;
        }
        if (s.combatPrepStep == CombatPrepStep.CHOOSE_MODE)
        {
            RunCommand("交战 自动");
        }
        else if (s.combatPrepStep == CombatPrepStep.CHOOSE_STANCE)
        {
            RunCommand("交战 接战");
        }
    }

    private void RefreshAll()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            SetStatus("模拟未启动 — 请从 Boot 进入战役");
            if (_phaseLabel != null)
            {
                _phaseLabel.text = "交战界面";
            }
            if (_bodyLabel != null)
            {
                _bodyLabel.text = "GameAppHost 未就绪";
            }
            return;
        }
        var s = core.State;
        var prep = s.phase == GamePhase.COMBAT_PREP;
        var chooseMode = s.combatPrepStep == CombatPrepStep.CHOOSE_MODE;
        var chooseStance = s.combatPrepStep == CombatPrepStep.CHOOSE_STANCE;
        var entry = CombatPhaseService.CurrentEntry(s);
        if (_phaseLabel != null)
        {
            _phaseLabel.text = prep ? "交战准备" : "自动交战";
        }
        if (_queueLabel != null)
        {
            var ord = entry != null && entry.queueOrdinal > 0
                ? entry.queueOrdinal + "/" + entry.queueTotal
                : s.combatQueueIndex + "/" + s.combatQueue.Count;
            _queueLabel.text = "队列 " + s.combatQueue.Count + " 项 · 当前 " + ord;
        }
        if (_bodyLabel != null)
        {
            _bodyLabel.text = entry?.label ?? (prep
                ? chooseMode
                    ? "请选择自动交战、参与战斗或舰队撤退。"
                    : chooseStance
                        ? "已选自动交战 · 请选择接战或撤退。"
                        : "交战处理中…"
                : s.lastCombatSummary ?? "自动结算执行中。");
        }
        var prepUi = prep && !s.spectatorMode;
        if (s.spectatorMode && _bodyLabel != null)
        {
            _bodyLabel.text = "观战模式 · 自动推进交战 · 全场景可见（实时阶段）";
        }
        if (_autoBtn != null)
        {
            _autoBtn.SetEnabled(prepUi && chooseMode);
        }
        if (_realtimeBtn != null)
        {
            _realtimeBtn.SetEnabled(prepUi && chooseMode);
        }
        if (_engageBtn != null)
        {
            _engageBtn.SetEnabled(prepUi);
        }
        if (_retreatBtn != null)
        {
            _retreatBtn.SetEnabled(prepUi);
        }
    }

    private void RunCommand(string line)
    {
        var msg = GameAppHost.Instance?.SubmitCommand(line) ?? "模拟未启动";
        SetStatus(msg);
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }
}
