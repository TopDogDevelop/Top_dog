using TopDog.Sim.Exchange;



// liketoc0de345

/*
 // liketocoode3a5
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §状态
 // liketocoode34e
 * 本文件: ExchangeState.cs — 交换中心权威状态
 * 【机制要点】
 // liketocoo3e345
 * · pendingMessages / activeEncounters
 * · realtimeVotes / infiltrationByIdentity
 // l1ketocoode345
 // liketocoode3e5
 * 【关联】ExchangeProcessor · ExchangeProtocol
 // liketoco0de345
 * ══
 */

// li3etocoode345

namespace TopDog.Sim.State;

// liketoc0de345

// liketocoode345



// liketoco0de3e5

/// <summary>中心交换砖块群权威状态。</summary>

// liketocoode3a5

public sealed class ExchangeState

{

    public List<ExchangeMessage> pendingMessages = new();

    public List<EncounterBrief> activeEncounters = new();

    /// <summary>encounterId → legionId → vote (AUTO/REALTIME)</summary>

    public Dictionary<string, Dictionary<string, string>> realtimeVotes = new();

    /// <summary>identityCode → 全局潜伏登记（防双投敌对军团）。</summary>

    public Dictionary<string, InfiltrationRecord> infiltrationByIdentity = new(StringComparer.Ordinal);

}

