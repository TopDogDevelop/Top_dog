using TopDog.Content;
using TopDog.Content.Banter;
using TopDog.Foundation.Io;
using TopDog.Sim.Banter;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BanterScriptCastWeaverTests
{
    private static MemberState Sheep(string suffix) => new()
    {
        memberId = BanterSheepDuckPhrases.IdentityCode + suffix,
        identityCode = BanterSheepDuckPhrases.IdentityCode,
        name = "绵羊伸腿",
    };

    private static MemberState BeiTou() => new()
    {
        memberId = "1000000801",
        identityCode = "10000008",
        name = BanterPersonalExclusiveLines.BeiTouJunShiName,
    };

    [SetUp]
    public void SetUp() => BanterDiagnosticLog.Clear();

    [Test]
    public void Opening_Slot1FirstInCastOrder_TriggersBackfill()
    {
        var sheepId = BanterSheepDuckPhrases.IdentityCode + "01";
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                Sheep("02"),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
                new MemberState { memberId = "1000000301", identityCode = "10000003", name = "路人" },
                new MemberState { memberId = "1000100201", identityCode = "10001002", name = "迪亚多啦" },
            },
            banterRuntime = new MemberBanterRuntimeState(),
        };
        var rt = state.banterRuntime!;
        rt.idleRosterSpeakerSlots[1] = sheepId;
        rt.idleRosterSpeakerSlots[2] = "1000000201";
        rt.idleRosterSpeakerSlots[3] = "1000000301";
        rt.idleCastDrawOrder = new List<string> { sheepId, "1000000201", "1000000301" };

        var queue = new List<BanterPlannedEmit>();
        BanterScriptCastWeaver.AppendOpeningExclusives(
            state,
            rt,
            BanterEligibleSpeakers.List(state),
            "test",
            0f,
            new Random(1),
            queue,
            "下一句");

        Assert.That(queue.Count, Is.GreaterThanOrEqualTo(1), "开场第一句应输出专属");
        Assert.That(queue[0].MemberId, Is.EqualTo(sheepId));
        Assert.That(rt.idleRosterSpeakerSlots[1], Is.Not.EqualTo(sheepId), "@1 第一句专属后应补抽顶班");
        Assert.That(rt.idleRosterSpeakerSlots[2], Is.EqualTo("1000000201"));
        Assert.That(rt.idleRosterSpeakerSlots[3], Is.EqualTo("1000000301"));
    }

    [Test]
    public void Opening_SheepAlwaysOptsOutAndBackfillsSlot()
    {
        var sheepId = BanterSheepDuckPhrases.IdentityCode + "01";
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
                new MemberState { memberId = "1000000301", identityCode = "10000003", name = "路人" },
                new MemberState { memberId = "1000100201", identityCode = "10001002", name = "迪亚多啦" },
            },
            banterRuntime = new MemberBanterRuntimeState(),
        };
        var rt = state.banterRuntime!;
        rt.idleRosterSpeakerSlots[3] = sheepId;
        rt.idleRosterSpeakerSlots[1] = "1000000201";
        rt.idleRosterSpeakerSlots[2] = "1000000301";
        rt.idleCastDrawOrder = new List<string> { "1000000201", "1000000301", sheepId };

        var queue = new List<BanterPlannedEmit>();
        BanterScriptCastWeaver.AppendOpeningExclusives(
            state,
            rt,
            BanterEligibleSpeakers.List(state),
            "test",
            0f,
            new Random(1),
            queue,
            "下一句");

        Assert.That(queue.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(BanterSheepDuckPhrases.Phrases, Does.Contain(queue[0].Text));
        Assert.That(rt.idleScriptOptOutMemberIds, Does.Contain(sheepId));
        Assert.That(rt.idleRosterSpeakerSlots[3], Is.Not.EqualTo(sheepId));
    }

    [Test]
    public void Opening_BeiTouMiss_KeepsScriptSlot()
    {
        var state = new GameState
        {
            members =
            {
                BeiTou(),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
            },
            banterRuntime = new MemberBanterRuntimeState(),
        };
        var rt = state.banterRuntime!;
        rt.idleRosterSpeakerSlots[1] = "1000000801";
        rt.idleCastDrawOrder = new List<string> { "1000000801" };

        for (var seed = 0; seed < 50; seed++)
        {
            rt.idleScriptOptOutMemberIds.Clear();
            rt.idleMandatoryLineSpokenIdentities.Clear();
            var queue = new List<BanterPlannedEmit>();
            BanterScriptCastWeaver.AppendOpeningExclusives(
                state,
                rt,
                BanterEligibleSpeakers.List(state),
                "test",
                0f,
                new Random(seed),
                queue,
                null);
            if (queue.Count == 0)
            {
                Assert.That(rt.idleRosterSpeakerSlots[1], Is.EqualTo("1000000801"));
                Assert.That(rt.idleScriptOptOutMemberIds, Is.Empty);
                return;
            }
        }

        Assert.Fail("No seed produced BeiTou opening miss within 50 tries");
    }

    [Test]
    public void PrepareScriptCast_NeverAssignsSameIdentityToMultipleSlots()
    {
        var members = new List<MemberState>
        {
            Sheep("01"),
            Sheep("02"),
            Sheep("03"),
            Sheep("04"),
            Sheep("05"),
            Sheep("06"),
            new MemberState { memberId = "1000100501", identityCode = "10001005", name = "羊村甲" },
            new MemberState { memberId = "1000100502", identityCode = "10001005", name = "羊村乙" },
            new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
        };
        var state = new GameState
        {
            members = members,
            banterRuntime = new MemberBanterRuntimeState(),
        };

        for (var seed = 0; seed < 500; seed++)
        {
            var rt = new MemberBanterRuntimeState();
            state.banterRuntime = rt;
            BanterScriptCastWeaver.PrepareScriptCast(state, rt, new Random(seed));

            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var kv in rt.idleRosterSpeakerSlots)
            {
                var member = members.Find(m => m.memberId == kv.Value);
                Assert.That(member, Is.Not.Null, $"seed={seed} slot={kv.Key}");
                var identity = IdentityCodes.Of(member!);
                Assert.That(identities.Add(identity), Is.True,
                    $"seed={seed} duplicate identity {identity} in slots");
            }
        }
    }

    [Test]
    public void BackfillSlot_NeverPicksSameIdentityAsOtherSlots()
    {
        var sheepId = BanterSheepDuckPhrases.IdentityCode + "01";
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                Sheep("02"),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
                new MemberState { memberId = "1000000301", identityCode = "10000003", name = "路人" },
                new MemberState { memberId = "1000100201", identityCode = "10001002", name = "迪亚多啦" },
            },
            banterRuntime = new MemberBanterRuntimeState(),
        };
        var rt = state.banterRuntime!;
        rt.idleRosterSpeakerSlots[1] = "1000000201";
        rt.idleRosterSpeakerSlots[2] = "1000000301";
        rt.idleRosterSpeakerSlots[3] = sheepId;
        rt.idleCastDrawOrder = new List<string> { "1000000201", "1000000301", sheepId };
        rt.idleScriptOptOutMemberIds.Add(sheepId);

        var queue = new List<BanterPlannedEmit>();
        BanterScriptCastWeaver.AppendOpeningExclusives(
            state,
            rt,
            BanterEligibleSpeakers.List(state),
            "test",
            0f,
            new Random(1),
            queue,
            "下一句");

        var backfillId = rt.idleRosterSpeakerSlots[3];
        Assert.That(backfillId, Is.Not.EqualTo(sheepId));
        Assert.That(backfillId, Does.Not.StartWith(BanterSheepDuckPhrases.IdentityCode),
            "backfill must not pick another sheep multibox account");
        Assert.That(backfillId, Is.Not.EqualTo("1000000201"));
        Assert.That(backfillId, Is.Not.EqualTo("1000000301"));
    }
}

[TestFixture]
public sealed class BanterCompanionOutputTests
{
    [Test]
    public void FormatLine_UsesNameColonBody()
    {
        Assert.That(BanterCompanionOutput.FormatLine("绵羊伸腿", "酱鸭"), Is.EqualTo("绵羊伸腿：酱鸭"));
    }
}

[TestFixture]
public sealed class BanterInlineMarkupTests
{
    [Test]
    public void Parse_ExampleLine_SplitsColorEmotesAndText()
    {
        var parsed = BanterInlineMarkupParser.Parse("#123/1那我要好好/1看看了/1");
        Assert.That(parsed.ColorId, Is.EqualTo(123));
        Assert.That(parsed.Runs.Count, Is.EqualTo(5));
        Assert.That(parsed.Runs[0].Kind, Is.EqualTo(BanterMarkupRunKind.Emote));
        Assert.That(parsed.Runs[0].EmoteId, Is.EqualTo(1));
        Assert.That(parsed.Runs[1].Text, Is.EqualTo("那我要好好"));
        Assert.That(parsed.Runs[2].Kind, Is.EqualTo(BanterMarkupRunKind.Emote));
        Assert.That(parsed.Runs[3].Text, Is.EqualTo("看看了"));
        Assert.That(parsed.Runs[4].Kind, Is.EqualTo(BanterMarkupRunKind.Emote));
    }
}

[TestFixture]
public sealed class BanterCatalogLoaderTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void LoadDefault_LoadsApprovedReactiveLines()
    {
        var catalog = BanterCatalogLoader.LoadDefault();
        Assert.That(catalog.ReactiveCommon.Any(l => l.EventKey == "equip_from_legion"), Is.True);
        Assert.That(catalog.ReactiveCommon.Any(l => l.Text.Contains("那我要好好")), Is.True);
        Assert.That(catalog.IdleGroups.ContainsKey("idle_ops_01"), Is.True);
        Assert.That(catalog.IdleGroups["idle_ops_01"].Count, Is.InRange(4, 5));
    }
}

[TestFixture]
public sealed class MemberBanterServiceTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
        BanterSignalHub.ClearForTests();
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void ReactiveCooldown_DropsSecondSignalWithinTwoSeconds()
    {
        var catalog = BanterCatalogLoader.LoadDefault();
        var svc = new MemberBanterService(catalog, seed: 1);
        var state = new GameState
        {
            members =
            {
                new MemberState { memberId = "1000000102", name = "新手教官", accountName = "奥法凯" },
            },
        };
        state.banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 999f };

        BanterSignalHub.Publish("equip_from_legion", "1000000102");
        svc.Tick(state, 0.1f, 1f);
        BanterSignalHub.Publish("equip_from_legion", "1000000102");
        svc.Tick(state, 0.1f, 1.2f);

        Assert.That(state.companionLog.Count, Is.EqualTo(1));
        Assert.That(state.companionLog[0].text, Does.Contain("教官"));
    }

    [Test]
    public void IdleEmitsAfterInterval()
    {
        var catalog = BanterCatalogLoader.LoadDefault();
        var svc = new MemberBanterService(catalog, seed: 2);
        var state = new GameState
        {
            members =
            {
                new MemberState { memberId = "1000000101", name = "奥法凯", accountName = "奥法凯" },
                new MemberState { memberId = "1000000201", name = "冰镇柠檬派" },
            },
        };
        state.banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 0f };

        svc.Tick(state, 0f, 0f);
        Assert.That(state.companionLog.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(state.companionLog[0].channel, Is.EqualTo("idle"));
    }
}

[TestFixture]
public sealed class DisplayLabelsBanterTests
{
    [Test]
    public void ResolveBanterSpeakerName_UsesGameNameNotAccountName()
    {
        var state = new GameState
        {
            members =
            {
                new MemberState
                {
                    memberId = "1000000102",
                    name = "新手教官",
                    accountName = "奥法凯",
                },
            },
        };

        Assert.That(DisplayLabels.ResolveBanterSpeakerName(state, "1000000102"), Is.EqualTo("新手教官"));
    }
}

[TestFixture]
public sealed class BanterDiagnosticIsolationTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
        BanterSignalHub.ClearForTests();
        CombatTelemetryLog.Clear();
        BanterDiagnosticLog.Clear();
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void CompanionEmit_DoesNotWriteCombatTelemetry()
    {
        var before = CombatTelemetryLog.DumpRecent();
        var catalog = BanterCatalogLoader.LoadDefault();
        var svc = new MemberBanterService(catalog);
        var state = new GameState
        {
            members = { new MemberState { memberId = "1000000102", name = "新手教官" } },
            banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 999f },
        };

        BanterSignalHub.Publish("equip_from_legion", "1000000102");
        svc.Tick(state, 0.1f, 1f);

        Assert.That(state.companionLog.Count, Is.EqualTo(1));
        Assert.That(CombatTelemetryLog.DumpRecent(), Is.EqualTo(before));
        Assert.That(BanterDiagnosticLog.Snapshot().Count, Is.GreaterThan(0));
        foreach (var line in state.alertLog)
        {
            Assert.That(line, Does.Not.Contain("教官"));
        }
    }
}

[TestFixture]
public sealed class BanterSheepDuckPhraseTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
        BanterSignalHub.ClearForTests();
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void SheepSpeaker_EmitsDuckPhrase_NotCatalogText()
    {
        var catalog = BanterCatalogLoader.LoadDefault();
        var svc = new MemberBanterService(catalog, seed: 7);
        var sheepId = BanterSheepDuckPhrases.IdentityCode + "01";
        var state = new GameState
        {
            members =
            {
                new MemberState
                {
                    memberId = sheepId,
                    identityCode = BanterSheepDuckPhrases.IdentityCode,
                    name = "绵羊伸腿",
                },
            },
            banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 999f },
        };

        BanterSignalHub.Publish("equip_from_legion", sheepId);
        svc.Tick(state, 0.1f, 1f);

        Assert.That(state.companionLog.Count, Is.EqualTo(1));
        Assert.That(BanterSheepDuckPhrases.Phrases, Does.Contain(state.companionLog[0].text));
        Assert.That(state.companionLog[0].text, Does.Not.Contain("教官"));
    }

    [Test]
    public void SheepDuckBag_CyclesAllFourBeforeRepeat()
    {
        var bag = new List<int>();
        var rng = new Random(3);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 4; i++)
        {
            seen.Add(BanterSheepDuckPhrases.DrawNext(bag, rng));
        }

        Assert.That(seen.Count, Is.EqualTo(4));
        Assert.That(BanterSheepDuckPhrases.Phrases, Is.SupersetOf(seen));
    }
}

[TestFixture]
public sealed class BanterSpeakerPickerTests
{
    private static MemberState M(string memberId, string identityCode) => new()
    {
        memberId = memberId,
        identityCode = identityCode,
        name = memberId,
    };

    [Test]
    public void Pick_AvoidsSameIdentityAsPreviousSpeaker()
    {
        var pool = new List<MemberState>
        {
            M("1000100101", "10001001"),
            M("1000100102", "10001001"),
            M("1000000201", "10000002"),
        };
        var rng = new Random(1);

        var picked = BanterSpeakerPicker.Pick(pool, "1000100101", null, null, rng);
        Assert.That(picked, Is.Not.Null);
        Assert.That(IdentityCodes.Of(picked!), Is.Not.EqualTo("10001001"));
    }

    [Test]
    public void Pick_AllowsSameIdentityWhenSplitMsgContinues()
    {
        var pool = new List<MemberState>
        {
            M("1000100101", "10001001"),
            M("1000100102", "10001001"),
            M("1000000201", "10000002"),
        };
        var rng = new Random(1);

        var picked = BanterSpeakerPicker.Pick(pool, "1000100101", "duck_split", "duck_split", rng);
        Assert.That(picked, Is.Not.Null);
        Assert.That(IdentityCodes.Of(picked!), Is.EqualTo("10001001"));
        Assert.That(picked!.memberId, Is.Not.EqualTo("1000100101"));
    }
}

[TestFixture]
public sealed class BanterMultiboxSyncTests
{
    private static MemberState M(string memberId, string identityCode, string? name = null) => new()
    {
        memberId = memberId,
        identityCode = identityCode,
        name = name ?? memberId,
    };

    [Test]
    public void CollectSyncSpeakers_AlwaysIncludesPrimary()
    {
        var pool = new List<MemberState> { M("1000000201", "10000002") };
        var ids = BanterMultiboxSync.CollectSyncSpeakers(pool, "1000000201", new Random(1));
        Assert.That(ids, Is.EqualTo(new[] { "1000000201" }));
    }

    [Test]
    public void CollectSyncSpeakers_TieredSpeakerCount()
    {
        var pool = new List<MemberState>
        {
            M("1000100101", "10001001", "绵羊伸腿"),
            M("1000100102", "10001001", "绵羊控股"),
            M("1000100103", "10001001", "绵羊制造"),
        };

        var counts = new Dictionary<int, int>();
        for (var seed = 0; seed < 20_000; seed++)
        {
            var ids = BanterMultiboxSync.CollectSyncSpeakers(pool, "1000100101", new Random(seed));
            counts[ids.Count] = counts.GetValueOrDefault(ids.Count) + 1;
        }

        Assert.That(counts.ContainsKey(1), Is.True);
        Assert.That(counts.ContainsKey(2), Is.True);
        Assert.That(counts.ContainsKey(3), Is.True);
        Assert.That(counts.ContainsKey(4), Is.False, "不应超过 3 个同 identity 号");
        Assert.That(counts[1] / 20_000.0, Is.InRange(0.85, 0.93), "1 号约 89%");
        Assert.That(counts.GetValueOrDefault(2) / 20_000.0, Is.InRange(0.07, 0.13), "2 号约 10%");
        Assert.That(counts.GetValueOrDefault(3) / 20_000.0, Is.InRange(0.003, 0.02), "3 号约 1%");
    }

    [Test]
    public void CollectSyncSpeakers_NeverExceedsThree_EvenWithManySiblings()
    {
        var pool = new List<MemberState>
        {
            M("1000100101", "10001001", "绵羊伸腿"),
            M("1000100102", "10001001", "绵羊控股"),
            M("1000100103", "10001001", "绵羊制造"),
            M("1000100104", "10001001", "羊村酱鸭"),
            M("1000100105", "10001001", "盖奶"),
            M("1000100106", "10001001", "绵羊没腿"),
        };

        for (var seed = 0; seed < 2000; seed++)
        {
            var ids = BanterMultiboxSync.CollectSyncSpeakers(pool, "1000100101", new Random(seed));
            Assert.That(ids.Count, Is.InRange(1, 3));
            Assert.That(ids, Does.Contain("1000100101"));
        }
    }

    [Test]
    public void Reactive_MultiboxSync_TieredRollsSameText()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")));
        BanterSignalHub.ClearForTests();

        var catalog = BanterCatalogLoader.LoadDefault();
        for (var seed = 0; seed < 2000; seed++)
        {
            BanterSignalHub.ClearForTests();
            var svc = new MemberBanterService(catalog, seed: seed);
            var state = new GameState
            {
                members =
                {
                    M("1000100101", "10001001", "绵羊伸腿"),
                    M("1000100102", "10001001", "绵羊控股"),
                    M("1000100103", "10001001", "绵羊制造"),
                },
                banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 999f },
            };

            BanterSignalHub.Publish("equip_from_legion", "1000100101");
            svc.Tick(state, 0.1f, 1f);

            if (state.companionLog.Count < 2)
            {
                continue;
            }

            Assert.That(state.companionLog.Select(e => e.text).Distinct().Count(), Is.EqualTo(1));
            Assert.That(state.companionLog.All(e => e.memberId.StartsWith("10001001", StringComparison.Ordinal)));
            Assert.That(state.companionLog.Count, Is.InRange(2, 3));
            Assert.That(state.banterReactiveCooldownSec.Keys.Count, Is.EqualTo(1));
            Assert.That(state.banterReactiveCooldownSec.ContainsKey("1000100101"), Is.True);
            return;
        }

        Assert.Fail("No seed triggered multibox sync within 2000 tries");
    }
}

[TestFixture]
public sealed class BanterIdleTimingTests
{
    [Test]
    public void CountTextChars_IgnoresEmoteMarkup()
    {
        Assert.That(BanterIdleTiming.CountTextChars("#123/1那我要好好/1看看了/1"), Is.EqualTo(8));
    }

    [Test]
    public void GapBeforeNextMessage_BasePlusCharDelay()
    {
        Assert.That(BanterIdleTiming.GapBeforeNextMessage("四字闲聊"), Is.EqualTo(2f + 4 * 0.2f));
    }

    [Test]
    public void IdleRound_SchedulesGapFromNextLineCharCount()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")));

        var catalog = BanterCatalogLoader.LoadDefault();
        var svc = new MemberBanterService(catalog, seed: 2);
        var state = new GameState
        {
            members =
            {
                new MemberState { memberId = "1000000101", name = "奥法凯" },
                new MemberState { memberId = "1000000201", name = "冰镇柠檬派" },
            },
            banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 0f },
        };

        svc.Tick(state, 0f, 0f);
        Assert.That(state.companionLog.Count, Is.EqualTo(1));

        var groupId = state.companionLog[0].groupId;
        Assert.That(groupId, Is.Not.Null);
        var lines = catalog.IdleGroups[groupId!];
        var nextLine = lines.Find(l => l.Seq == 2);
        Assert.That(nextLine, Is.Not.Null);
        var expectedGap = BanterIdleTiming.GapBeforeNextMessage(nextLine!.Text);
        Assert.That(state.banterRuntime!.idleNextEmitSec, Is.EqualTo(expectedGap).Within(0.001f));
    }
}

[TestFixture]
public sealed class BanterMandatoryLineRulesTests
{
    private static MemberState Sheep(string suffix, string? name = null) => new()
    {
        memberId = BanterSheepDuckPhrases.IdentityCode + suffix,
        identityCode = BanterSheepDuckPhrases.IdentityCode,
        name = name ?? "绵羊伸腿",
    };

    private static BanterCatalog CatalogWithSingleIdleGroup()
    {
        var catalog = new BanterCatalog();
        var group = new List<IdleBanterLine>();
        for (var seq = 1; seq <= 4; seq++)
        {
            group.Add(new IdleBanterLine
            {
                GroupId = "test_sheep_burst",
                Seq = seq,
                MemberId = "*",
                Text = $"闲聊句{seq}",
            });
        }

        catalog.IdleGroups["test_sheep_burst"] = group;
        return catalog;
    }

    [Test]
    public void Pick_ExcludesIdentityThatAlreadySpokeMandatoryLine()
    {
        var pool = new List<MemberState>
        {
            Sheep("01"),
            Sheep("02", "绵羊控股"),
            new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
        };
        var excluded = new HashSet<string>(StringComparer.Ordinal) { BanterSheepDuckPhrases.IdentityCode };
        var picked = BanterSpeakerPicker.Pick(pool, null, null, null, new Random(1), excluded);
        Assert.That(picked, Is.Not.Null);
        Assert.That(IdentityCodes.Of(picked!), Is.EqualTo("10000002"));
    }

    [Test]
    public void Idle_ExclusiveSpeaker_OutputsDuckAndMarksRound()
    {
        var catalog = CatalogWithSingleIdleGroup();
        var svc = new MemberBanterService(catalog, seed: 1);
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                Sheep("02", "绵羊控股"),
                Sheep("03", "绵羊制造"),
            },
            banterRuntime = new MemberBanterRuntimeState
            {
                idleNextEmitSec = 0f,
                idleGroupId = "test_sheep_burst",
                idleNextSeq = 1,
                idleGroupLineCount = 4,
            },
        };

        svc.Tick(state, 0f, 0f);

        Assert.That(state.companionLog.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(BanterSheepDuckPhrases.Phrases, Does.Contain(state.companionLog[0].text));
        Assert.That(state.companionLog.Select(e => e.text).Distinct().Count(), Is.EqualTo(1));
        Assert.That(
            state.banterRuntime!.idleMandatoryLineSpokenIdentities,
            Does.Contain(BanterSheepDuckPhrases.IdentityCode));
    }

    [Test]
    public void Idle_ExclusiveSpeaker_MultiboxSync_TieredRates()
    {
        var catalog = CatalogWithSingleIdleGroup();
        var multiHits = 0;
        var singleHits = 0;
        for (var seed = 0; seed < 2000; seed++)
        {
            var svc = new MemberBanterService(catalog, seed: seed);
            var state = new GameState
            {
                members =
                {
                    Sheep("01"),
                    Sheep("02", "绵羊控股"),
                    Sheep("03", "绵羊制造"),
                },
                banterRuntime = new MemberBanterRuntimeState
                {
                    idleNextEmitSec = 0f,
                    idleGroupId = "test_sheep_burst",
                    idleNextSeq = 1,
                    idleGroupLineCount = 4,
                },
            };

            svc.Tick(state, 0f, 0f);
            svc.Tick(state, 0f, BanterIdleTiming.MultiboxEchoGapSec + 0.01f);
            if (state.companionLog.Count >= 2)
            {
                multiHits++;
            }
            else if (state.companionLog.Count == 1
                     && BanterSheepDuckPhrases.Phrases.Contains(state.companionLog[0].text))
            {
                singleHits++;
            }
        }

        Assert.That(multiHits, Is.GreaterThan(0));
        Assert.That(singleHits, Is.GreaterThan(multiHits));
    }

    [Test]
    public void Idle_SecondWildcardPick_DoesNotRepeatMandatoryLine()
    {
        var catalog = CatalogWithSingleIdleGroup();
        var svc = new MemberBanterService(catalog, seed: 1);
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                Sheep("02", "绵羊控股"),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
            },
            banterRuntime = new MemberBanterRuntimeState
            {
                idleNextEmitSec = 0f,
                idleGroupId = "test_sheep_burst",
                idleNextSeq = 1,
                idleGroupLineCount = 4,
            },
        };

        svc.Tick(state, 0f, 0f);
        var rt = state.banterRuntime!;
        var drainThrough = rt.idleEmitQueue.Count > 0
            ? rt.idleEmitQueue[^1].EmitAtSec + 0.01f
            : BanterIdleTiming.GapBeforeNextMessage("闲聊句2") + 1f;
        svc.Tick(state, 0f, drainThrough);

        var duckCount = state.companionLog.Count(e => BanterSheepDuckPhrases.Phrases.Contains(e.text));
        Assert.That(duckCount, Is.InRange(1, 3), "多开跟读各号分别输出同一句");
        Assert.That(state.companionLog.Any(e => e.text == "闲聊句2"), Is.True);
    }

    [Test]
    public void Idle_ExclusiveSpeaker_ContinuesCatalogWhenExclusiveAlreadySpoken()
    {
        var catalog = CatalogWithSingleIdleGroup();
        var svc = new MemberBanterService(catalog, seed: 1);
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                Sheep("02", "绵羊控股"),
            },
            banterRuntime = new MemberBanterRuntimeState
            {
                idleNextEmitSec = 0f,
                idleGroupId = "test_sheep_burst",
                idleNextSeq = 1,
                idleGroupLineCount = 4,
                idleMandatoryLineSpokenIdentities = { BanterSheepDuckPhrases.IdentityCode },
            },
        };

        svc.Tick(state, 0f, 0f);

        Assert.That(state.companionLog.Count, Is.EqualTo(1));
        Assert.That(state.companionLog[0].text, Is.EqualTo("闲聊句1"));
    }

    [Test]
    public void Idle_ExclusiveSpeaker_SameSlotLaterLine_ContinuesAfterExclusive()
    {
        var sheepId = BanterSheepDuckPhrases.IdentityCode + "01";
        var backfillId = "1000100201";
        var catalog = new BanterCatalog();
        catalog.IdleGroups["test_sheep_slot"] = new List<IdleBanterLine>
        {
            new() { GroupId = "test_sheep_slot", Seq = 1, MemberId = "@1", Text = "剧本开场" },
            new() { GroupId = "test_sheep_slot", Seq = 2, MemberId = "@2", Text = "他人接话" },
            new() { GroupId = "test_sheep_slot", Seq = 3, MemberId = "@1", Text = "剧本收尾" },
        };
        var svc = new MemberBanterService(catalog, seed: 3);
        var state = new GameState
        {
            members =
            {
                Sheep("01"),
                new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
                new MemberState { memberId = "1000000301", identityCode = "10000003", name = "路人" },
                new MemberState { memberId = backfillId, identityCode = "10001002", name = "迪亚多啦" },
            },
            banterRuntime = new MemberBanterRuntimeState
            {
                idleNextEmitSec = 0f,
                idleGroupId = "test_sheep_slot",
                idleNextSeq = 1,
                idleGroupLineCount = 3,
                idleRosterSpeakerSlots =
                {
                    [1] = sheepId,
                    [2] = "1000000201",
                    [3] = "1000000301",
                },
                idleCastDrawOrder = { sheepId, "1000000201", "1000000301" },
            },
        };

        svc.Tick(state, 0f, 0f);
        var rt = state.banterRuntime!;
        var drainThrough = rt.idleEmitQueue.Count > 0
            ? rt.idleEmitQueue[^1].EmitAtSec + 0.01f
            : 20f;
        svc.Tick(state, 0f, drainThrough);

        Assert.That(BanterSheepDuckPhrases.Phrases, Does.Contain(state.companionLog[0].text));
        Assert.That(state.companionLog[0].memberId, Is.EqualTo(sheepId));
        Assert.That(rt.idleScriptOptOutMemberIds, Does.Contain(sheepId));
        Assert.That(rt.idleRosterSpeakerSlots[1], Is.EqualTo(backfillId));
        Assert.That(state.companionLog.Any(e => e.memberId == backfillId && e.text == "剧本开场"), Is.True);
        Assert.That(state.companionLog.Any(e => e.memberId == backfillId && e.text == "剧本收尾"), Is.True);
    }
}

[TestFixture]
public sealed class BanterBeiTouExclusiveTests
{
    private static MemberState BeiTou(string memberId = "1000000801") => new()
    {
        memberId = memberId,
        identityCode = "10000008",
        name = BanterPersonalExclusiveLines.BeiTouJunShiName,
    };

    private static BanterCatalog CatalogWithSingleIdleGroup()
    {
        var catalog = new BanterCatalog();
        catalog.IdleGroups["test_beitou"] = new List<IdleBanterLine>
        {
            new()
            {
                GroupId = "test_beitou",
                Seq = 1,
                MemberId = "1000000801",
                Text = "闲聊句一",
            },
        };
        return catalog;
    }

    [Test]
    public void TryResolveForIdle_BeiTou_RollsExclusiveHalfTheTime()
    {
        var member = BeiTou();
        var rt = new MemberBanterRuntimeState();
        var exclusive = 0;
        var catalog = 0;
        for (var seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            BanterPersonalExclusiveLines.TryResolveForIdle(
                member,
                rt,
                "闲聊句一",
                rng,
                out var text,
                out var used);
            if (used && text == BanterPersonalExclusiveLines.BeiTouRecruitLine)
            {
                exclusive++;
            }
            else if (!used && text == "闲聊句一")
            {
                catalog++;
            }
        }

        Assert.That(exclusive, Is.InRange(70, 130));
        Assert.That(catalog, Is.InRange(70, 130));
    }

    [Test]
    public void Idle_BeiTou_WhenExclusive_UsesRecruitLineAndMarksRound()
    {
        var catalog = CatalogWithSingleIdleGroup();
        catalog.IdleGroups["test_beitou"].AddRange(new[]
        {
            new IdleBanterLine { GroupId = "test_beitou", Seq = 2, MemberId = "*", Text = "闲聊句二" },
            new IdleBanterLine { GroupId = "test_beitou", Seq = 3, MemberId = "*", Text = "闲聊句三" },
            new IdleBanterLine { GroupId = "test_beitou", Seq = 4, MemberId = "*", Text = "闲聊句四" },
        });
        for (var seed = 0; seed < 100; seed++)
        {
            var svc = new MemberBanterService(catalog, seed: seed);
            var state = new GameState
            {
                members =
                {
                    BeiTou(),
                    new MemberState { memberId = "1000000201", identityCode = "10000002", name = "他人" },
                },
                banterRuntime = new MemberBanterRuntimeState
                {
                    idleNextEmitSec = 0f,
                    idleGroupId = "test_beitou",
                    idleNextSeq = 1,
                    idleGroupLineCount = 4,
                },
            };

            svc.Tick(state, 0f, 0f);
            if (state.companionLog.Count == 0)
            {
                continue;
            }

            if (state.companionLog[0].text == BanterPersonalExclusiveLines.BeiTouRecruitLine)
            {
                Assert.That(
                    state.banterRuntime!.idleMandatoryLineSpokenIdentities,
                    Does.Contain("10000008"));
                return;
            }
        }

        Assert.Fail("No seed rolled BeiTou exclusive within 100 tries");
    }

    [Test]
    public void Idle_BeiTou_WhenNotExclusive_SkipsLine()
    {
        var catalog = CatalogWithSingleIdleGroup();
        for (var seed = 0; seed < 100; seed++)
        {
            var svc = new MemberBanterService(catalog, seed: seed);
            var state = new GameState
            {
                members = { BeiTou() },
                banterRuntime = new MemberBanterRuntimeState
                {
                    idleNextEmitSec = 0f,
                    idleGroupId = "test_beitou",
                    idleNextSeq = 1,
                    idleGroupLineCount = 1,
                },
            };

            svc.Tick(state, 0f, 0f);
            if (state.companionLog.Count == 0)
            {
                Assert.That(state.banterRuntime!.idleNextEmitSec, Is.GreaterThan(0f));
                Assert.That(state.banterRuntime.idleMandatoryLineSpokenIdentities, Is.Empty);
                return;
            }
        }

        Assert.Fail("No seed rolled BeiTou catalog miss within 100 tries");
    }
}
