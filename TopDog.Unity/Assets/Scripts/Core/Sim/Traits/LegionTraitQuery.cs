using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Traits;

/// <summary>军团内是否存在某词条（按现实人 identity 去重）。</summary>
public static class LegionTraitQuery
{
    public static bool LegionHasTrait(GameState state, string? legionId, string traitId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return false;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            if (!legionId.Equals(m.legionId, StringComparison.Ordinal))
            {
                continue;
            }
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code) || !seen.Add(code))
            {
                continue;
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
