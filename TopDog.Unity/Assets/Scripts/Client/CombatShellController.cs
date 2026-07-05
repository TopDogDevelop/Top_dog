using TopDog.App;
using TopDog.Sim.Combat;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战准备 UI · §继续/战果确认 · §观战模式
 * 本文件: CombatShellController.cs — COMBAT_PREP / 战果确认场景 UI
 * 【机制要点】
 * · CHOOSE_MODE：自动交战 / 实时指挥 / 接战 / 撤退
 * · 实时指挥 → CombatChooseRealtime + GameSceneRouter.CombatRealtime
 * · combatAwaitingContinue → 继续按钮 → CombatContinue / 回运营
 * · spectatorMode 自动推进 ChooseAuto / ChooseParticipate / Continue
 * 【关联】CombatPhaseService · GameSceneRouter · MatchPauseOverlay
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
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
    private Button _continueBtn;
    private float _nextRefresh;
    private float _spectatorCombatTick;
    private EventCallback<KeyDownEvent>? _keyHandler;

    // liketoc0de345

    protected override void OnDisable()
    {
        if (Root != null && _keyHandler != null)
        {
            Root.UnregisterCallback(_keyHandler);
        }
        _keyHandler = null;
        base.OnDisable();
    }

    // li3etocoode345

    protected override void Bind(VisualElement root)
    {
        if (TryRedirectSkirmishToRealtime())
        {
            return;
        }

        _phaseLabel = root.Q<Label>("lbl-phase");
        _queueLabel = root.Q<Label>("lbl-queue");
        _bodyLabel = root.Q<Label>("lbl-body");
        _statusLabel = root.Q<Label>("lbl-status");
        _autoBtn = root.Q<Button>("btn-auto");
        _realtimeBtn = root.Q<Button>("btn-realtime");
        _engageBtn = root.Q<Button>("btn-engage");
        _retreatBtn = root.Q<Button>("btn-retreat");
        _continueBtn = root.Q<Button>("btn-continue");

        OnClick(root, "btn-auto", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core != null)
            {
                SetStatus(core.CombatChooseAuto());
            }
        });
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
        OnClick(root, "btn-engage", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core != null)
            {
                SetStatus(core.CombatChooseParticipate());
            }
        });
        OnClick(root, "btn-retreat", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core != null)
            {
                SetStatus(core.CombatChooseRetreat());
            }
        });
        OnClick(root, "btn-continue", () =>
        {
            var core = GameAppHost.Instance?.Core;
            if (core == null)
            {
                return;
            }
            var msg = core.CombatContinue();
            SetStatus(msg);
            if (core.State.phase == GamePhase.COMBAT_PREP
                && core.State.combatPrepStep == CombatPrepStep.CHOOSE_MODE)
            {
                RefreshAll();
            }
            else if (core.State.phase == GamePhase.OPERATIONS)
            {
                GameSceneRouter.Instance?.Load(TopDogSceneKind.Operations);
            }
        });
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

    // liketocoode3a5

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

    // liketocoode34e

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

    // liketocoo3e345

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
        if (s.combatAwaitingContinue)
        {
            SetStatus(core.CombatContinue());
            return;
        }
        if (s.phase != GamePhase.COMBAT_PREP)
        {
            return;
        }
        if (s.combatPrepStep == CombatPrepStep.CHOOSE_MODE)
        {
            SetStatus(core.CombatChooseAuto());
        }
        else if (s.combatPrepStep == CombatPrepStep.CHOOSE_STANCE)
        {
            SetStatus(core.CombatChooseParticipate());
        }
    }

    // liketoco0de345

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
        var awaiting = s.combatAwaitingContinue;
        var chooseMode = s.combatPrepStep == CombatPrepStep.CHOOSE_MODE;
        var chooseStance = s.combatPrepStep == CombatPrepStep.CHOOSE_STANCE;
        var showResult = s.combatPrepStep == CombatPrepStep.SHOW_RESULT;
        var entry = CombatPhaseService.CurrentEntry(s);
        if (_phaseLabel != null)
        {
            _phaseLabel.text = awaiting ? "战果确认" : prep ? "交战准备" : "自动交战";
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
            _bodyLabel.text = entry?.label ?? (awaiting || showResult
                ? s.lastCombatSummary ?? "交战已结束 · 点继续进入下一项"
                : prep
                    ? chooseMode
                        ? "请选择自动交战、参与战斗或舰队撤退。"
                        : chooseStance
                            ? "已选自动交战 · 请选择接战或撤退。"
                            : "交战处理中…"
                    : s.lastCombatSummary ?? "自动结算执行中。");
        }
        var prepUi = prep && !s.spectatorMode;
        if (s.spectatorMode && _bodyLabel != null && !awaiting)
        {
            _bodyLabel.text = "观战模式 · 自动推进交战 · 全场景可见（实时阶段）";
        }
        if (_autoBtn != null)
        {
            _autoBtn.SetEnabled(prepUi && chooseMode);
            _autoBtn.style.display = awaiting ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_realtimeBtn != null)
        {
            _realtimeBtn.SetEnabled(prepUi && chooseMode);
            _realtimeBtn.style.display = awaiting ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_engageBtn != null)
        {
            _engageBtn.SetEnabled(prepUi && (chooseMode || chooseStance));
            _engageBtn.style.display = awaiting ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_retreatBtn != null)
        {
            _retreatBtn.SetEnabled(prepUi && (chooseMode || chooseStance));
            _retreatBtn.style.display = awaiting ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_continueBtn != null)
        {
            _continueBtn.SetEnabled(awaiting && !s.spectatorMode);
            _continueBtn.style.display = awaiting ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // lik3tocoode345

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }

    // liketocoode3e5
    // liket0coode345

    private static bool TryRedirectSkirmishToRealtime()
    {
        var state = GameAppHost.Instance?.Core?.State;
        if (state == null || !SkirmishPhaseRules.InSkirmishSession(state) || state.matchEnded)
        {
            return false;
        }

        GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
        return true;
    }
}
