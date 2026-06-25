namespace TopDog.Content.Traits;

public sealed class TraitDef
{
    public string? traitId;
    public string? displayNameZh;
    public string? displayNameEn;
    public string? mechanismId;
    public Dictionary<string, object>? @params;
    public int resolutionOrder = 5;
    public string resolutionPhase = "post_ops_pre_combat";
    public bool unique;
    public string? stackingPolicy;
    /// <summary>为 false 时不出现在招新随机/默认词条池；仍可通过开局预设、机制、演化等赋予。</summary>
    public bool recruitPool = true;
    public List<string>? presentationTags;
}
