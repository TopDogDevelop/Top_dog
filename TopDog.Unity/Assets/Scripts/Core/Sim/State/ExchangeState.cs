using TopDog.Sim.Exchange;



namespace TopDog.Sim.State;



/// <summary>中心交换砖块群权威状态。</summary>

public sealed class ExchangeState

{

    public List<ExchangeMessage> pendingMessages = new();

    public List<EncounterBrief> activeEncounters = new();

    /// <summary>encounterId → legionId → vote (AUTO/REALTIME)</summary>

    public Dictionary<string, Dictionary<string, string>> realtimeVotes = new();

    /// <summary>identityCode → 全局潜伏登记（防双投敌对军团）。</summary>

    public Dictionary<string, InfiltrationRecord> infiltrationByIdentity = new(StringComparer.Ordinal);

}

