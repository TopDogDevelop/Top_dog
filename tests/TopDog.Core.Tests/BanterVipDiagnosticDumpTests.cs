using System.Text;
using TopDog.Content.Banter;
using TopDog.Content.Starting;
using TopDog.Foundation.Io;
using TopDog.Sim.Banter;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
[Explicit("Manual VIP banter diagnostic dump")]
public sealed class BanterVipDiagnosticDumpTests
{
    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void DumpVipRounds_ToFile()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
        var catalog = BanterCatalogLoader.LoadDefault();
        var members = StartingTemplateLoader.LoadMembers("template_vip_invest");
        var sb = new StringBuilder();
        var sheepPrefix = BanterSheepDuckPhrases.IdentityCode;

        for (var seed = 0; seed < 12; seed++)
        {
            BanterDiagnosticLog.Clear();
            var svc = new MemberBanterService(catalog, seed: seed);
            var state = new GameState
            {
                members = members,
                banterRuntime = new MemberBanterRuntimeState { idleNextEmitSec = 0f },
            };

            svc.Tick(state, 0f, 0f);
            var rt = state.banterRuntime!;
            var drain = rt.idleEmitQueue.Count > 0 ? rt.idleEmitQueue[^1].EmitAtSec + 0.01f : 60f;
            svc.Tick(state, 0f, drain);

            var groupId = state.banterRuntime!.idleGroupId ?? "(finished)";
            var pickedGroup = "";
            var diag = BanterDiagnosticLog.Snapshot();
            var roundStart = diag.FirstOrDefault(l => l.Contains("round-start group="));
            if (roundStart != null)
            {
                var start = roundStart.IndexOf("group=", StringComparison.Ordinal) + 6;
                var end = roundStart.IndexOf(' ', start);
                if (end > start) groupId = roundStart[start..end];
            }

            var usesSlots = catalog.IdleGroups.TryGetValue(groupId, out var lines)
                && lines.Any(l => BanterRosterSpeakers.IsSlot(l.MemberId));

            sb.AppendLine($"=== seed={seed} group={groupId} usesSlots={usesSlots} companion={state.companionLog.Count} ===");
            foreach (var e in state.companionLog)
            {
                var isSheep = e.memberId?.StartsWith(sheepPrefix, StringComparison.Ordinal) == true;
                var name = members.FirstOrDefault(m => m.memberId == e.memberId)?.name ?? e.memberId;
                sb.AppendLine($"  companion: {name}: {e.text}{(isSheep ? " [SHEEP]" : "")}");
            }

            foreach (var line in BanterDiagnosticLog.Snapshot())
            {
                sb.AppendLine($"  {line}");
            }

            sb.AppendLine();
        }

        var outPath = Path.Combine(RepoRoot(), "banter-vip-debug-dump.txt");
        File.WriteAllText(outPath, sb.ToString());
        TestContext.WriteLine($"Wrote {outPath}");
        Assert.That(File.Exists(outPath));
    }
}
