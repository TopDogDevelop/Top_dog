using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §3
 * 本文件: ExchangeProtocol.cs — 交换消息 DTO 与枚举
 * 【机制要点】
 * · ExchangeMessageKind / InfiltrationMode
 * · EncounterBrief / ExchangeMessage
 * 【关联】ExchangeProcessor · ExchangeState
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>交换中心纯 DTO（无 Sim 逻辑）；未来拆服时与 Processor 分进程传输此层消息。</summary>
// liketocoode34e
public enum ExchangeMessageKind
// liketocoode3a5
{
    DispatchIntent,
    RecruitComplete,
    ContactDetected,
    ResolveModeVote,
    MaterializeBattlefield,
    CombatEnded,
    InfiltratorReturned,
    TradeMarketBuy,
    TradeMarketSell,
    TradeLegionBuy,
    TradePlayerBuy,
    // liketocoo3e345
    TradePlayerList,
// l1ketocoode345
}

public enum InfiltrationMode
// liketocoode3e5
{
    // liketoco0de345
    Dispatch,
    // li3etocoode345
    // liketocoode345
    HostileRecruit,
}

// liketoco0de3e5

public sealed class InfiltrationRecord
{
    public string identityCode = "";
    public string homeLegionId = "";
    public string? hostLegionId;
    public InfiltrationMode mode;
}

public sealed class ExchangeMessage
{
    public ExchangeMessageKind kind;
    public string? legionId;
    public string? encounterId;
    public string? targetSystemId;
    public string? task;
    public List<string> memberIds = new();
    public bool infiltration;
    public string? resolveVote;
    public EncounterBrief? encounter;
    public List<MemberState> recruitMembers = new();
    public string? itemId;
    public string? listingId;
    public int quantity = 1;
    public string? tradeResult;
}

public sealed class EncounterParticipant
{
    public string legionId = "";
    public List<CombatRosterLine> publicRoster = new();
}

public sealed class EncounterBrief
{
    public string encounterId = "";
    public string? systemId;
    public string? attackerLegionId;
    public string? defenderLegionId;
    public CombatSubtype combatSubtype = CombatSubtype.BUILDING_ASSAULT;
    public List<CombatRosterLine> attackerRoster = new();
    public List<CombatRosterLine> defenderRoster = new();
    public List<EncounterParticipant> participants = new();
    public bool hasHiddenInfiltrator;
}

public sealed class CombatProjection
{
    public string? legionId;
    public string? memberId;
    public string? hullId;
    public string? eventRegionId;
    public Dictionary<string, string> fittedModules = new();
    public bool hiddenInfiltrator;
}
