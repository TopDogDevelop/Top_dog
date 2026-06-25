using System.Collections.Generic;

namespace TopDog.Client.StarMap;

public enum FortSovereignty
{
    None = 0,
    FriendlyUnanchored = 1,
    FriendlyAnchored = 2,
    Enemy = 3,
}

public sealed class StarMapSystemBadge
{
    public string? systemId;
    public string? displayName;
    public int playerBuildingCount;
    public int taskMemberCount;
    public bool playerPresence;
    public bool hostilePresence;
    public int activeBattlefieldCount;
    public FortSovereignty fortSovereignty;
    public readonly List<string> memberNames = new();
}
