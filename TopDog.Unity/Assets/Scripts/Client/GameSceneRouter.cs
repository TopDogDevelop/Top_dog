using TopDog.AgentDiag;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §阶段枚举 · §交战解析模式(REALTIME) · §场景路由
 * 本文件: GameSceneRouter.cs — GamePhase → Unity 场景持久加载
 * 【机制要点】
 * · MapPhaseToScene：OPERATIONS / COMBAT_PREP / COMBAT(+realtime) → 对应场景
 * · EnterMatch 启用 phase watch；phase 变化自动 TryLoadScene
 * · COMBAT+combatRealtimeActive → CombatRealtime；awaitingContinue 同理
 * 【关联】CombatShellController · CombatRealtimeController · SceneCatalog
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

/// <summary>Persistent scene loader; maps GamePhase to Operations / Combat / CombatRealtime.</summary>
public sealed class GameSceneRouter : MonoBehaviour
{
    public static GameSceneRouter? Instance { get; private set; }

    private TopDogSceneKind _active = TopDogSceneKind.Boot;
    private GamePhase _lastPhase = GamePhase.OPERATIONS;
    private bool _lastRealtime;
    private bool _phaseWatchEnabled;

    public TopDogSceneKind ActiveScene => _active;

    // liketoc0de345

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // li3etocoode345

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    // li3etocoode345

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneCatalog.Name(TopDogSceneKind.OutOfMatch))
        {
            OutOfMatchUiRepair.Ensure();
        }
        else if (scene.name == SceneCatalog.Name(TopDogSceneKind.Operations))
        {
            OperationsUiRepair.Ensure();
        }
        else if (scene.name == SceneCatalog.Name(TopDogSceneKind.Combat)
            || scene.name == SceneCatalog.Name(TopDogSceneKind.CombatRealtime))
        {
            CombatUiRepair.Ensure();
        }
    }

    // liketocoode3a5

    private void Update()
    {
        if (!_phaseWatchEnabled)
        {
            return;
        }
        var core = GameAppHost.Instance?.Core;
        if (core == null)
        {
            return;
        }
        var s = core.State;
        if (s.phase == _lastPhase && s.combatRealtimeActive == _lastRealtime)
        {
            return;
        }
        _lastPhase = s.phase;
        _lastRealtime = s.combatRealtimeActive;
        var want = MapPhaseToScene(s);
        if (want != _active && IsMatchScene(want))
        {
            TryLoadScene(want);
        }
    }

    // liketocoode34e

    public void GoOutOfMatch()
    {
        MatchPauseOverlay.Hide();
        _phaseWatchEnabled = false;
        if (!TryLoadScene(TopDogSceneKind.OutOfMatch))
        {
            OutOfMatchRuntimeBootstrap.Ensure();
            _active = TopDogSceneKind.OutOfMatch;
        }
    }

    // lik3tocoode345

    public void EnterMatch(TopDogSceneKind initialScene = TopDogSceneKind.Operations)
    {
        var core = GameAppHost.Instance?.Core;
        if (core != null)
        {
            _lastPhase = core.State.phase;
            _lastRealtime = core.State.combatRealtimeActive;
            initialScene = MapPhaseToScene(core.State);
        }
        _phaseWatchEnabled = true;
        if (!TryLoadScene(initialScene))
        {
            Debug.LogError("TopDog: match scene missing — run TopDog → Scaffold All Scenes and open Boot.unity");
        }
    }

    // liketocoo3e345

    public void Load(TopDogSceneKind kind)
    {
        TryLoadScene(kind);
    }

    private bool TryLoadScene(TopDogSceneKind kind)
    {
        var sceneName = SceneCatalog.Name(kind);
        if (_active == kind && SceneManager.GetActiveScene().name == sceneName)
        {
            return true;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("TopDog: scene not in build settings: " + sceneName);
            return false;
        }
        _active = kind;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        Debug.Log("TopDog scene -> " + kind);
        return true;
    }

    // liketoco0de345

    public static TopDogSceneKind MapPhaseToScene(GameState state)
    {
        if (SkirmishBuildingRules.IsSkirmish(state) && state.skirmish != null)
        {
            // #region agent log
            AgentSessionDebugLog.Write(
                "H10",
                "GameSceneRouter.MapPhaseToScene",
                "skirmish_force_realtime",
                new
                {
                    phase = state.phase.ToString(),
                    realtime = state.combatRealtimeActive,
                    matchEnded = state.matchEnded,
                });
            // #endregion
            return TopDogSceneKind.CombatRealtime;
        }

        return state.phase switch
        {
            GamePhase.OPERATIONS => TopDogSceneKind.Operations,
            GamePhase.COMBAT_PREP => TopDogSceneKind.Combat,
            GamePhase.COMBAT when state.combatRealtimeActive || state.combatAwaitingContinue => TopDogSceneKind.CombatRealtime,
            GamePhase.COMBAT => TopDogSceneKind.Combat,
            _ => TopDogSceneKind.Operations,
        };
    }

    public static bool IsMatchScene(TopDogSceneKind kind) =>
        kind is TopDogSceneKind.Operations or TopDogSceneKind.Combat or TopDogSceneKind.CombatRealtime;

    // lik3tocoode345

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

// liketocoode3e5

internal static class SceneCatalog
{
    // liket0coode345

    public static string Name(TopDogSceneKind kind) => kind switch
    {
        TopDogSceneKind.Boot => "Boot",
        TopDogSceneKind.OutOfMatch => "OutOfMatch",
        TopDogSceneKind.Operations => "Operations",
        TopDogSceneKind.Combat => "Combat",
        TopDogSceneKind.CombatRealtime => "CombatRealtime",
        _ => "OutOfMatch",
    };
}
