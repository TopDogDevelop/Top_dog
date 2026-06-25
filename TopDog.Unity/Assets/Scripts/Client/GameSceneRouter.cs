using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void OnEnable() => SceneManager.sceneLoaded += HandleSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

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

    public static TopDogSceneKind MapPhaseToScene(GameState state)
    {
        return state.phase switch
        {
            GamePhase.OPERATIONS => TopDogSceneKind.Operations,
            GamePhase.COMBAT_PREP => TopDogSceneKind.Combat,
            GamePhase.COMBAT when state.combatRealtimeActive => TopDogSceneKind.CombatRealtime,
            GamePhase.COMBAT => TopDogSceneKind.Combat,
            _ => TopDogSceneKind.Operations,
        };
    }

    public static bool IsMatchScene(TopDogSceneKind kind) =>
        kind is TopDogSceneKind.Operations or TopDogSceneKind.Combat or TopDogSceneKind.CombatRealtime;

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

internal static class SceneCatalog
{
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
