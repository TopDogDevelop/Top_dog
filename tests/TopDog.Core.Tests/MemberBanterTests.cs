using TopDog.Content;
using TopDog.Content.Banter;
using TopDog.Foundation.Io;
using TopDog.Sim.Banter;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

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
    public void TryCollectSyncBurst_ReturnsAllSiblingsInPool()
    {
        var pool = new List<MemberState>
        {
            M("1000100101", "10001001", "绵羊伸腿"),
            M("1000100102", "10001001", "绵羊控股"),
            M("1000100103", "10001001", "绵羊制造"),
            M("1000000201", "10000002", "他人"),
        };

        Assert.That(
            BanterMultiboxSync.TryCollectSyncBurst(pool, "1000100102", out var burst),
            Is.True);
        Assert.That(burst, Is.EqualTo(new[] { "1000100101", "1000100102", "1000100103" }));
    }

    [Test]
    public void TryCollectSyncBurst_FailsWithSingleAccount()
    {
        var pool = new List<MemberState> { M("1000000201", "10000002") };
        Assert.That(
            BanterMultiboxSync.TryCollectSyncBurst(pool, "1000000201", out _),
            Is.False);
    }

    [Test]
    public void Reactive_MultiboxSync_EmitsSameTextForAllSiblings()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..")));
        BanterSignalHub.ClearForTests();

        var catalog = BanterCatalogLoader.LoadDefault();
        for (var seed = 0; seed < 500; seed++)
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

            if (state.companionLog.Count < 3)
            {
                continue;
            }

            Assert.That(state.companionLog.Select(e => e.text).Distinct().Count(), Is.EqualTo(1));
            Assert.That(
                state.companionLog.Select(e => e.memberId).OrderBy(x => x, StringComparer.Ordinal),
                Is.EqualTo(new[] { "1000100101", "1000100102", "1000100103" }));
            Assert.That(state.banterReactiveCooldownSec.Keys.Count, Is.EqualTo(1));
            Assert.That(state.banterReactiveCooldownSec.ContainsKey("1000100101"), Is.True);
            return;
        }

        Assert.Fail("No seed triggered multibox sync within 500 tries");
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
    public void Idle_ExclusiveSpeaker_MultiboxSync_RemainsTenPercent()
    {
        var catalog = CatalogWithSingleIdleGroup();
        var syncHits = 0;
        var singleHits = 0;
        for (var seed = 0; seed < 500; seed++)
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
            if (state.companionLog.Count >= 3)
            {
                syncHits++;
            }
            else if (state.companionLog.Count == 1
                     && BanterSheepDuckPhrases.Phrases.Contains(state.companionLog[0].text))
            {
                singleHits++;
            }
        }

        Assert.That(syncHits, Is.GreaterThan(0));
        Assert.That(singleHits, Is.GreaterThan(syncHits));
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
        var firstGap = state.banterRuntime!.idleNextEmitSec;
        svc.Tick(state, 0f, firstGap + 0.01f);

        var duckCount = state.companionLog.Count(e => BanterSheepDuckPhrases.Phrases.Contains(e.text));
        Assert.That(duckCount, Is.InRange(1, 3), "Exclusive duck emit may 10% sync multibox");
        Assert.That(state.companionLog.Any(e => e.text == "闲聊句2"), Is.True);
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
    public void Idle_BeiTou_WhenNotExclusive_UsesCatalogText()
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
            if (state.companionLog.Count == 1 && state.companionLog[0].text == "闲聊句一")
            {
                Assert.That(
                    state.banterRuntime!.idleMandatoryLineSpokenIdentities,
                    Is.Empty);
                return;
            }
        }

        Assert.Fail("No seed rolled BeiTou catalog within 100 tries");
    }
}
