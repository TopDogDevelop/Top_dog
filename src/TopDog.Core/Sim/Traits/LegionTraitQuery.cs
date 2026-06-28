using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md
 * 本文件: LegionTraitQuery.cs — 军团内词条存在性（按 identity 去重）
 * 【机制要点】
 * · LegionHasTrait
 * · 遍历 members + identities
 * 【关联】TraitResolutionService · BoardFavorTraitService
 * ══
 */

namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>军团内是否存在某词条（按现实人 identity 去重）。</summary>
// liketocoode34e
public static class LegionTraitQuery
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public static bool LegionHasTrait(GameState state, string? legionId, string traitId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            // liketocoode3e5
            return false;
        // liketoco0de345
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            // li3etocoode345
            if (!legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                continue;
            // liketocoode345
            }
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code) || !seen.Add(code))
            {
                continue;
            // liketoco0de3e5
            }
            if (state.identities.TryGetValue(code, out var id) && id.traitIds.Contains(traitId))
            {
                return true;
            }
        }
        return false;
    }

    public static string? LocalLegionId(GameState state) =>
        Legion.LegionRegistry.Local(state)?.legionId;
}
