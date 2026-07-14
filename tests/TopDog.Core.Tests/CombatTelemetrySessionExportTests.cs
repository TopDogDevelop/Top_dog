using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatTelemetrySessionExportTests
{
    private string _tmpRoot = "";

    [SetUp]
    public void SetUp()
    {
        _tmpRoot = Path.Combine(Path.GetTempPath(), "td_combat_export_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpRoot);
        CombatTelemetryLog.Clear();
        CombatTelemetrySessionExport.ResetForTests();
        CombatTelemetrySessionExport.Enabled = true;
        CombatTelemetrySessionExport.LatestPath = Path.Combine(_tmpRoot, "latest.log");
        CombatTelemetrySessionExport.ArchiveDir = Path.Combine(_tmpRoot, "archive");
    }

    [TearDown]
    public void TearDown()
    {
        CombatTelemetrySessionExport.End("teardown");
        CombatTelemetrySessionExport.ResetForTests();
        CombatTelemetryLog.Clear();
        try
        {
            if (Directory.Exists(_tmpRoot))
            {
                Directory.Delete(_tmpRoot, recursive: true);
            }
        }
        catch
        {
            // ignore
        }
    }

    [Test]
    public void Begin_CapturesPreFit_And_FullSalvo_OnEnd()
    {
        CombatTelemetryLog.Log(
            "combat.fit",
            "u1 铁棺级 mods=[mod_hybrid_gun_xl] salvo=6000 cycle=15.0s range=100km track=0.5°/s");

        var state = new GameState
        {
            campaignName = "场域·灰狼·威慑炮",
            mechanismTest = new TopDog.Sim.MechanismTest.MechanismTestMatchState
            {
                scenarioId = "mt_field_aura",
            },
        };
        state.worldline.type = WorldlineType.STORY;

        CombatTelemetrySessionExport.Begin(state);
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.True);

        CombatTelemetryLog.LogSalvo(
            new BattlefieldUnit { displayName = "铁棺级" },
            new BattlefieldUnit { displayName = "灰狼级" },
            6000f,
            15f,
            6000f);
        CombatTelemetryLog.LogSalvo(
            new BattlefieldUnit { displayName = "铁棺级" },
            new BattlefieldUnit { displayName = "灰狼级" },
            6000f,
            15f,
            5000f);

        var path = CombatTelemetrySessionExport.End("test");
        Assert.That(path, Is.Not.Null.And.EqualTo(CombatTelemetrySessionExport.LatestPath));
        Assert.That(File.Exists(path!), Is.True);
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.False);

        var text = File.ReadAllText(path!);
        Assert.That(text, Does.Contain("mode=mechanism:mt_field_aura"));
        Assert.That(text, Does.Contain("[combat.fit]"));
        Assert.That(text, Does.Contain("[combat.salvo] 铁棺级→灰狼级"));
        Assert.That(text, Does.Contain("summary.salvo attacker=铁棺级 count=2"));
        Assert.That(text, Does.Contain("cycles=[15.0,15.0]"));
        Assert.That(text, Does.Contain("summary.field-route"));
        Assert.That(text, Does.Contain("summary.heal"));

        var archive = CombatTelemetrySessionExport.LastArchivePath;
        Assert.That(archive, Is.Not.Null);
        Assert.That(File.Exists(archive!), Is.True);
    }

    [Test]
    public void EnsureActive_Reopens_After_Accidental_End()
    {
        var state = new GameState { combatRealtimeActive = true };
        CombatTelemetrySessionExport.Begin(state);
        CombatTelemetrySessionExport.End("accident");
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.False);

        CombatTelemetrySessionExport.EnsureActive(state);
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.True);
        CombatTelemetryLog.Log("combat.salvo", "probe→x round=1 cycle=1.0s applied=1");
        var path = CombatTelemetrySessionExport.End("ok");
        var text = File.ReadAllText(path!);
        Assert.That(text, Does.Contain("reopened"));
        Assert.That(text, Does.Contain("[combat.salvo] probe→x"));
    }

    [Test]
    public void LinkService_Begin_Starts_Session()
    {
        var state = new GameState();
        CombatRealtimeLinkService.Begin(state);
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.True);
        Assert.That(state.combatRealtimeLinkHandshakeActive, Is.True);
        CombatRealtimeLinkService.Reset(state);
        Assert.That(CombatTelemetrySessionExport.IsActive, Is.False);
        Assert.That(File.Exists(CombatTelemetrySessionExport.LatestPath), Is.True);
    }
}
