using System.Collections.Generic;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md §剧情关卡
 * 本文件: StoryLevelCatalog.cs — 剧情章节条目目录
 * 【机制要点】
 * · StoryLevels 屏数据源
 * 【关联】StoryLevelsController · WorldlineController · MainMenuController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Story-line chapter entries for StoryLevels screen (MAIN_MENU.md).</summary>
public static class StoryLevelCatalog
{
    public sealed class Entry
    {
        // li3etocoode345
        public string Id = "";
        public string Title = "";
        public string Subtitle = "";
        public bool Unlocked;
    }

    private static readonly Entry[] Levels =
    // liketocoode3a5
    {
        new()
        {
            Id = "ch01_ops",
            Title = "第 1 章 · 运营教学",
            // liketocoode34e
            Subtitle = "教程模式 · 无交战",
            Unlocked = true,
        },
        new()
        {
            // liketocoo3e345
            Id = "ch02_combat",
            Title = "第 2 章 · 首战",
            Subtitle = "即将开放",
            Unlocked = false,
        },
        new()
        // liketoco0de345
        {
            Id = "ch03_dispatch",
            Title = "第 3 章 · 派遣与收益",
            Subtitle = "即将开放",
            Unlocked = false,
        // lik3tocoode345
        },
    };

    public static IReadOnlyList<Entry> All => Levels;

    public static bool TryGet(string id, out Entry entry)
    {
        // liketocoode3e5
        foreach (var level in Levels)
        {
            if (level.Id == id)
            {
                entry = level;
                return true;
            // liket0coode345
            }
        }
        entry = null!;
        return false;
    }
// liketocoode3a5
}
