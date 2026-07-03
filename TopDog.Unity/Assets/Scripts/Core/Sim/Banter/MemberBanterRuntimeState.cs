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
    /// <summary>剧本组角色槽 @1/@2/@3 → memberId（编织后最终指派，含补抽）。</summary>
    public Dictionary<int, string> idleRosterSpeakerSlots = new();
    /// <summary>本轮 cast 抽取顺序（开场专属按此顺序处理）。</summary>
    public List<string> idleCastDrawOrder = new();
    /// <summary>开场专属后退出剧本的 memberId。</summary>
    public HashSet<string> idleScriptOptOutMemberIds = new(StringComparer.Ordinal);
    /// <summary>本轮动态占位符缓存（仅地点跨句一致）。</summary>
    public BanterIdleDynamicContext? idleDynamicContext;
    /// <summary>闲聊轮次盐（每开新组递增，打散随机）。</summary>
    public int banterRoundSalt;
    /// <summary>当前闲聊轮预排输出队列（先随机后排期）。</summary>
    public List<BanterPlannedEmit> idleEmitQueue = new();
    /// <summary>下一待投递的 <see cref="idleEmitQueue"/> 下标。</summary>
    public int idleEmitQueueIndex;
}
