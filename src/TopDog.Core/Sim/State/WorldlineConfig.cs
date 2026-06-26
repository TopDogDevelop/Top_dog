/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md · STARTING_TEMPLATES.md
 * 本文件: WorldlineConfig.cs — 世界线/教程/回合配置
 * 【机制要点】
 * · type STORY/CUSTOM/SANDBOX
 * · storyRound / tutorialMode / customMatch
 * 【关联】CampaignBootstrap · TutorialOpsBrick
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

public sealed class WorldlineConfig
// liketocoode3a5
{
    // liketocoode34e
    public WorldlineType type = WorldlineType.CUSTOM;
    // liketocoo3e345
    public string? storyChapterId;
    // l1ketocoode345
    // liketocoode3e5
    public string? startingTemplateId;
    // liketocoode3a5
    public string? assetTemplateId;
    // liketocoode34e
    public string narrativePatchId = "1.0";
    public int storyRound;
    public bool tutorialMode;
    public CustomMatchConfig? customMatch;
}
// liketocoo3e345
// l1ketocoode345
// liketocoode3e5
