using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Trigger;

public static class TriggerConditions
{
    public static bool Passes(
        GameState state,
        Dictionary<string, string>? conditions,
        TraitResolutionContext? resolution = null)
    {
        if (conditions == null || conditions.Count == 0)
        {
            return true;
        }
        foreach (var kv in conditions)
        {
            if (!PassesOne(state, kv.Key, kv.Value, resolution))
            {
                return false;
            }
        }
        return true;
    }

    private static bool PassesOne(
        GameState state,
        string key,
        string? expected,
        TraitResolutionContext? resolution)
    {
        if (key.Equals("phase", StringComparison.OrdinalIgnoreCase))
        {
            return state.phase.ToString().Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
        if (key.Equals("resolutionPhase", StringComparison.OrdinalIgnoreCase))
        {
            return resolution != null
                   && resolution.resolutionPhase != null
                   && resolution.resolutionPhase.Equals(expected, StringComparison.Ordinal);
        }
        if (key.Equals("mechanismId", StringComparison.OrdinalIgnoreCase))
        {
            return resolution != null
                   && resolution.mechanismId != null
                   && resolution.mechanismId.Equals(expected, StringComparison.Ordinal);
        }
        if (key.Equals("traitId", StringComparison.OrdinalIgnoreCase))
        {
            return resolution != null
                   && resolution.traitId != null
                   && resolution.traitId.Equals(expected, StringComparison.Ordinal);
        }
        if (key.Equals("identityCode", StringComparison.OrdinalIgnoreCase))
        {
            return resolution != null
                   && resolution.identityCode != null
                   && resolution.identityCode.Equals(expected, StringComparison.Ordinal);
        }
        if (key.Equals("hasTrait", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(expected))
        {
            foreach (var id in state.identities.Values)
            {
                if (id.traitIds.Contains(expected))
                {
                    return true;
                }
            }
            foreach (var m in state.members)
            {
                if (m.traitIds.Contains(expected))
                {
                    return true;
                }
            }
            return false;
        }
        if (key.Equals("identityHasTrait", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(expected)
            && resolution?.identityCode != null
            && state.identities.TryGetValue(resolution.identityCode, out var identity))
        {
            return identity.traitIds.Contains(expected);
        }
        if (key.Equals("flag", StringComparison.OrdinalIgnoreCase))
        {
            var parts = (expected ?? "").Split('=', 2);
            if (parts.Length == 2)
            {
                return state.flags.TryGetValue(parts[0], out var actual)
                       && actual.Equals(parts[1], StringComparison.Ordinal);
            }
            return state.flags.ContainsKey(expected ?? "");
        }
        return true;
    }
}
