using System.Collections.Generic;

namespace TopDog.Client;

/// <summary>Story-line chapter entries for StoryLevels screen (MAIN_MENU.md).</summary>
public static class StoryLevelCatalog
{
    public sealed class Entry
    {
        public string Id = "";
        public string Title = "";
        public string Subtitle = "";
        public bool Unlocked;
    }

    private static readonly Entry[] Levels =
    {
        new()
        {
            Id = "ch01_ops",
            Title = "第 1 章 · 运营教学",
            Subtitle = "教程模式 · 无交战",
            Unlocked = true,
        },
        new()
        {
            Id = "ch02_combat",
            Title = "第 2 章 · 首战",
            Subtitle = "即将开放",
            Unlocked = false,
        },
        new()
        {
            Id = "ch03_dispatch",
            Title = "第 3 章 · 派遣与收益",
            Subtitle = "即将开放",
            Unlocked = false,
        },
    };

    public static IReadOnlyList<Entry> All => Levels;

    public static bool TryGet(string id, out Entry entry)
    {
        foreach (var level in Levels)
        {
            if (level.Id == id)
            {
                entry = level;
                return true;
            }
        }
        entry = null!;
        return false;
    }
}
