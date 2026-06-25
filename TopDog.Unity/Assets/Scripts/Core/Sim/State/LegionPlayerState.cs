namespace TopDog.Sim.State;

/// <summary>单军团玩家私有域（名册、仓库、编队、招新）。</summary>
public sealed class LegionPlayerState
{
    public string legionId = "";
    public List<MemberState> members = new();
    public List<FormationState> formations = new();
    public Dictionary<string, int> legionStock = new();
    public float recruitProgressSec;
    public List<string> recruitTargetTraitIds = new();
    public string lastRecruitSummary = "";
    /// <summary>招新完成待 Exchange <c>RecruitComplete</c> 提交的成员（CUSTOM 战役）。</summary>
    public List<MemberState> pendingRecruits = new();
}
