using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

/// <summary>Headless mt_field_aura 90s — 打印铁棺/场域/维修摘要（人工核对用）。</summary>
[TestFixture]
public sealed class MtFieldAuraCombatExportDiagTests
{
    [Test]
    public void MtFieldAura_90s_Writes_Export_Summary()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "td_mt3_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        CombatTelemetryLog.Clear();
        CombatTelemetrySessionExport.ResetForTests();
        CombatTelemetrySessionExport.Enabled = true;
        CombatTelemetrySessionExport.LatestPath = Path.Combine(tmp, "latest.log");
        CombatTelemetrySessionExport.ArchiveDir = Path.Combine(tmp, "arch");

        var core = CampaignBootstrap.CreateFromMechanismTest("mt_field_aura");
        var state = core.State;
        state.combatRealtimeLinkHandshakeActive = false;
        state.combatRealtimeLinkDelaySec = 0f;
        state.autoFireEnabled = true;

        var mods = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        for (var i = 0; i < 90; i++)
        {
            BattlefieldSystem.Tick(state, mods, ships, 1f);
        }

        var path = CombatTelemetrySessionExport.End("diag");
        Assert.That(path, Is.Not.Null);
        var text = File.ReadAllText(path!);
        TestContext.Out.WriteLine(text);

        Assert.That(text, Does.Contain("mode=mechanism:mt_field_aura"));
        Assert.That(text, Does.Contain("hull=hull_dread_ironcoffin"));
        Assert.That(text, Does.Contain("mod_hybrid_gun_xl"));
        // 双 XL：设计单门 6000/15s → 汇总 round=12000 cycle=15
        Assert.That(text, Does.Match(@"hull=hull_dread_ironcoffin.*salvo=12000.*cycle=15"));
        Assert.That(text, Does.Contain("summary.salvo"));
        // 威慑炮不应再以 cycle=10 主炮齐射双开
        Assert.That(text, Does.Not.Contain("羊村星猫→").Or.Not.Match(@"羊村星猫→.*cycle=10\.0s"));
        var starcatFit = text.Split('\n').FirstOrDefault(l => l.Contains("mod_deterrence_gun_yl"));
        Assert.That(starcatFit, Is.Not.Null);
        Assert.That(starcatFit!, Does.Contain("salvo=0")); // 专用武器不进主炮 profile

        CombatTelemetrySessionExport.ResetForTests();
        try
        {
            Directory.Delete(tmp, true);
        }
        catch
        {
            // ignore
        }
    }
}
