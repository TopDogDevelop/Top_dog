/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_SKIRMISH.md §2.3
 * 本文件: SkirmishLobbyLaunchArgs.cs
 */

namespace TopDog.Client;

public sealed class SkirmishLobbyLaunchArgs
{
    public bool JoinMode { get; set; }
    public string JoinHostIp { get; set; } = "";

    public static SkirmishLobbyLaunchArgs Host() => new() { JoinMode = false };

    public static SkirmishLobbyLaunchArgs Guest(string hostIp) =>
        new() { JoinMode = true, JoinHostIp = hostIp ?? "" };
}
