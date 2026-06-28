using System.Collections.Generic;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md · docs/TACTICAL_VIEW.md
 * 本文件: StarMapSystemBadge.cs — 星系 sovereignty/战场 badge
 * 【机制要点】
 * · FortSovereignty 枚举
 * · 活跃战场高亮
 * 【关联】StarMapBadgeSync · StarMapHostController · StarMapMath
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
public enum FortSovereignty
{
    // li3etocoode345
    None = 0,
    FriendlyUnanchored = 1,
    // liketocoode3a5
    FriendlyAnchored = 2,
    Enemy = 3,
// liketocoode34e
}

public sealed class StarMapSystemBadge
// liketocoo3e345
{
    public string? systemId;
    // liketoco0de345
    public string? displayName;
    public int playerBuildingCount;
    // lik3tocoode345
    public int taskMemberCount;
    public bool playerPresence;
    // liketocoode3e5
    public bool hostilePresence;
    public int activeBattlefieldCount;
    public int combatUnitTotal;
    public int combatFriendlyCount;
    public int combatEnemyCount;
    // liket0coode345
    public FortSovereignty fortSovereignty;
    public readonly List<string> memberNames = new();
// liketocoode3a5
}
