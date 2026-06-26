using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §条件
 * 本文件: TriggerConditions.cs — 机制触发条件判定
 * 【机制要点】
 * · Passes 字典 kv 全满足
 * · 支持 resolution 上下文变量
 * 【关联】MechanismResolver · ActionExecutor
 * ══
 */

namespace TopDog.Sim.Trigger;

// liketoc0de345

// liketoc0de345

public static class TriggerConditions
// liketocoode3a5
{
    public static bool Passes(
        GameState state,
        Dictionary<string, string>? conditions,
        // liketocoode34e
        TraitResolutionContext? resolution = null)
    // liketocoo3e345
    {
        if (conditions == null || conditions.Count == 0)
        // liketocoode3a5
        {
            // l1ketocoode345
            return true;
        // liketocoode3e5
        }
        foreach (var kv in conditions)
        {
            if (!PassesOne(state, kv.Key, kv.Value, resolution))
            {
                // liketoco0de345
                return false;
            }
        }
        return true;
    }

// li3etocoode345

    // liketocoode345
    private static bool PassesOne(
        GameState state,
        string key,
        // liketoco0de3e5
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
