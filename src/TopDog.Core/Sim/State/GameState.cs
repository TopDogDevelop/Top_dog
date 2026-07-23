using TopDog.Content.Map;
using TopDog.Sim.Alliance;
using TopDog.Sim.Banter;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;

using TopDog.Sim.Exchange;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md · CODEBASE_MAP.md
 * 本文件: GameState.cs — 权威可序列化战役状态根
 * 【机制要点】
 * · legions / members / exchange / battlefields
 * · phase / map / worldline / combatQueue
 * 【关联】SaveCodec · SimulationCore
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Authoritative campaign state (serializable).</summary>
// liketocoode34e
public sealed class GameState
// liketocoo3e345
{
    // l1ketocoode345
    // liketocoode3e5
    public int schemaVersion = 6;
    // liketoco0de345
    public string campaignName = "Campaign";
    // li3etocoode345
    // liketocoode345
    public WorldlineConfig worldline = new();
    // liketoco0de3e5
    public GamePhase phase = GamePhase.OPERATIONS;
    // liketocoode3a5
    public LoadedMap? map;
    public string? currentSolarSystemId;
    public List<LegionState> legions = new();
    public Dictionary<string, LegionPlayerState> legionPlayers = new(StringComparer.Ordinal);
    public ExchangeState exchange = new();
    public List<MemberState> members = new();
    public List<ShipInstanceState> shipInstances = new();
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
    /// <summary>战术相机注视单位（切换视野）；不要求可附身。</summary>
    public string? tacticalCameraUnitId;
    public CombatResolveMode? pendingResolveMode;
    public bool combatRealtimeActive;
    /// <summary>实时战场连接握手中（画面可见、模拟冻结）。</summary>
    public bool combatRealtimeLinkHandshakeActive;
    /// <summary>握手剩余秒；-1 表示未在握手。</summary>
    public float combatRealtimeLinkDelaySec = -1f;
    public bool autoFireEnabled;
    /// <summary>战术跃迁默认落点距中心（米，1–1000 km）；单舰 warpLandingDistM 可覆盖。</summary>
    public float tacticalWarpLandingDistM = TacticalWarpLandingService.DefaultLandingDistM;
    public List<TacticalWarpTransitEntry> tacticalWarpInTransit = new();
    /// <summary>战术导航白点世界坐标（当前 active 战场）。</summary>
    public float tacticalNavX;
    public float tacticalNavY;
    public float tacticalNavZ;
    public bool tacticalNavVisible;
    /// <summary>无框选时舰队命令范围（TACTICAL_NAVIGATION.md）。</summary>
    public FleetCommandScope fleetCommandScope = FleetCommandScope.AllInScene;
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

    public List<BattleReportRecord> battleReports = new();

    public Skirmish.SkirmishMatchState? skirmish;

    public MechanismTest.MechanismTestMatchState? mechanismTest;

    public List<CompanionLogEntry> companionLog = new();
    public Dictionary<string, float> banterReactiveCooldownSec = new(StringComparer.Ordinal);
    public MemberBanterRuntimeState? banterRuntime;

    /// <summary>登录夺舍：本局内不可再重生的团员 memberId。</summary>
    public HashSet<string> boardingPermadeadMemberIds = new(StringComparer.Ordinal);

    /// <summary>对局开始时舰体/装配快照（重生回滚、登录不跨复活继承）。</summary>
    public Dictionary<string, MemberMatchBaseline> matchMemberBaselines = new(StringComparer.Ordinal);

    /// <summary>董事会召来：下一场友方战场生成时从施法者放出 5 翼。</summary>
    public string? pendingBoardSummonIdentityCode;
    public string? pendingBoardSummonLegionId;
    public string? pendingBoardSummonCasterMemberId;

    /// <summary>董事会召来：战术选中友方舰；空则随机军团在场舰。</summary>
    public string? pendingBoardSummonTargetUnitId;

    /// <summary>策划支援已揭露的内鬼现实人 identityCode。</summary>
    public HashSet<string> revealedInfiltratorIdentityCodes = new();
}
