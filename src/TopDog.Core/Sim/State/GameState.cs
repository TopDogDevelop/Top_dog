using TopDog.Content.Map;
using TopDog.Sim.Alliance;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;

using TopDog.Sim.Exchange;

namespace TopDog.Sim.State;

/// <summary>Authoritative campaign state (serializable).</summary>
public sealed class GameState
{
    public int schemaVersion = 5;
    public string campaignName = "Campaign";
    public WorldlineConfig worldline = new();
    public GamePhase phase = GamePhase.OPERATIONS;
    public LoadedMap? map;
    public string? currentSolarSystemId;
    public List<LegionState> legions = new();
    public Dictionary<string, LegionPlayerState> legionPlayers = new(StringComparer.Ordinal);
    public ExchangeState exchange = new();
    public List<MemberState> members = new();
    public List<FormationState> formations = new();
    public List<FleetState> fleets = new();
    public List<LegionAssetState> legionAssets = new();
    public float operationTimeRemainingSec;
    public float operationDurationSec = 300f;
    public int gameYear = 1;
    public int gameWeek = 1;
    public int tutorialStep;
    public bool tutorialComplete;
    public Dictionary<string, string> flags = new();
    public List<string> alertLog = new();
    public string lastCommandEcho = "";

    public List<CombatQueueEntry> combatQueue = new();
    public int combatQueueIndex;
    public CombatPrepStep combatPrepStep = CombatPrepStep.CHOOSE_MODE;
    public bool combatAwaitingContinue;
    public bool aiAgreedAutoResolve;
    public string lastCombatSummary = "";
    public int storyRound;
    public List<OpponentHarvestOp> opponentHarvestOps = new();

    public List<BuildingState> buildings = new();
    public List<string> aiPendingAssaultBuildingIds = new();
    public List<AiPendingAssaultOp> aiPendingAssaults = new();
    public List<PlayerPendingAssaultOp> playerPendingAssaults = new();
    /// <summary>已发起约战的星系；该星系内任意 NORMAL/FRAGILE 建筑可再被攻击。</summary>
    public List<string> activeSiegeSystemIds = new();
    /// <summary>当前 <see cref="App.SimulationCore.SubmitCommand"/> 执行上下文（皮套/联机）。</summary>
    public string? commandIssuerLegionId;
    public string? pendingBuildingChoiceId;

    public float emptyCombatNoticeSec;
    public bool emptyCombatPending;

    public float recruitProgressSec;
    public List<string> recruitTargetTraitIds = new();
    public string lastRecruitSummary = "";
    public long nextIdentityCode = 10000001L;
    public int recruitBatchSeq;

    public Dictionary<string, int> legionStock = new();
    public Dictionary<string, Dictionary<string, int>> personalStockByGroup = new();
    public Dictionary<string, Dictionary<string, string>> memberFittedModules = new();
    public Dictionary<string, string> memberActivePropulsionSlot = new();

    public List<PresentationDirective> presentationQueue = new();

    public List<BattlefieldState> battlefields = new();
    public string? activeBattlefieldId;
    public string? possessingMemberId;
    public CombatResolveMode? pendingResolveMode;
    public bool combatRealtimeActive;
    public bool autoFireEnabled = true;
    public float possessionYawInput;
    public float possessionPitchInput;
    public bool possessionToggleThrottle;
    public Dictionary<string, float> aiRetargetCooldownSec = new();

    public AllianceState? playerAlliance;

    public Dictionary<string, IdentityState> identities = new();
    public MarketState market = new();

    public string? commanderIdentityCode;
    public int commanderLastDismissStoryRound = -999;
    public string? campaignOutcome;
    public bool spectatorMode;
    public bool matchEnded;
    public string? matchWinnerLegionId;
    public bool creditsDismissed;
    public bool spectatorFullVision;
    public List<string> matchAppearedIdentityCodes = new();
    public int peakLegionCount;
    public List<string> legionFortressEliminatedLegionIdsThisCombatRound = new();

    /// <summary>董事会召来：下一场友方战场生成时注入无畏增援。</summary>
    public string? pendingBoardSummonIdentityCode;
    public string? pendingBoardSummonLegionId;

    /// <summary>策划支援已揭露的内鬼现实人 identityCode。</summary>
    public HashSet<string> revealedInfiltratorIdentityCodes = new();
}
