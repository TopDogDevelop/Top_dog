namespace TopDog.Sim.Banter;

public sealed class MemberBanterRuntimeState
{
    public float idleNextEmitSec;
    public string? idleGroupId;
    public int idleNextSeq = 1;
    public int idleGroupLineCount;
    /// <summary>闲聊组内上一句发言人 memberId（伪随机去同现实人接话）。</summary>
    public string? idleLastSpeakerMemberId;
    /// <summary>闲聊组内上一行 splitMsgId（多号拼一句时允许多开同 identity）。</summary>
    public string? idleLastSplitMsgId;
    /// <summary>绵羊伸腿酱鸭四句洗牌袋。</summary>
    public List<int> sheepDuckPhraseBag = new();
    /// <summary>本轮已说过专属台词的 identityCode（每现实人每轮至多一次）。</summary>
    public HashSet<string> idleMandatoryLineSpokenIdentities = new(StringComparer.Ordinal);
}
