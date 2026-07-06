using System.Collections.Generic;
using TopDog.Sim.MechanismTest;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md §剧情关卡 · docs/MECHANISM_TEST_INDEX.md
 * 本文件: StoryLevelCatalog.cs — 剧情章节 + 机制详测关卡目录
 * ══
 */

namespace TopDog.Client;

public static class StoryLevelCatalog
{
    public sealed class Entry
    {
        public string Id = "";
        public string Title = "";
        public string Subtitle = "";
        public bool Unlocked;
        public bool IsMechanismTest;
    }

    private static readonly Entry[] StoryLevels =
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

    public static IReadOnlyList<Entry> All
    {
        get
        {
            var list = new List<Entry>(StoryLevels);
            foreach (var scenario in MechanismTestCatalog.ListAll())
            {
                var orderLabel = scenario.scenarioOrder > 0
                    ? $"#{scenario.scenarioOrder:D2} · "
                    : "";
                list.Add(new Entry
                {
                    Id = scenario.scenarioId,
                    Title = "机制详测 " + orderLabel + scenario.displayName,
                    Subtitle = "单矿带 20km · 直进实时战",
                    Unlocked = true,
                    IsMechanismTest = true,
                });
            }

            return list;
        }
    }

    public static bool TryGet(string id, out Entry entry)
    {
        foreach (var level in All)
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
