using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

public static class OpsDeploymentHelper
{
    public static List<MemberState> PickEncounterParticipants(
        GameState state,
        string? battlefieldSystemId,
        int maxCount,
        Random rng)
    {
        var mandatory = new List<MemberState>();
        var optional = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (MustAttendSystemCombat(m, battlefieldSystemId))
            {
                mandatory.Add(m);
            }
            else
            {
                optional.Add(m);
            }
        }
        optional = optional.OrderByDescending(m => DeployScore(m, battlefieldSystemId)).ToList();
        var picked = new List<MemberState>(mandatory);
        foreach (var m in optional)
        {
            if (picked.Count >= maxCount)
            {
                break;
            }
            if (!picked.Contains(m))
            {
                picked.Add(m);
            }
        }
        var count = Math.Min(maxCount, Math.Max(1, state.members.Count));
        if (picked.Count < count)
        {
            foreach (var m in optional)
            {
                if (picked.Count >= count)
                {
                    break;
                }
                if (!picked.Contains(m))
                {
                    picked.Add(m);
                }
            }
        }
        if (picked.Count > count && mandatory.Count < count)
        {
            picked = picked.Take(count).ToList();
        }
        if ((float)rng.NextDouble() < 0.3f && optional.Count > 0)
        {
            var wild = optional[rng.Next(optional.Count)];
            if (!picked.Contains(wild))
            {
                var replaceIdx = -1;
                for (var i = 0; i < picked.Count; i++)
                {
                    if (!MustAttendSystemCombat(picked[i], battlefieldSystemId))
                    {
                        replaceIdx = i;
                        break;
                    }
                }
                if (replaceIdx >= 0)
                {
                    picked[replaceIdx] = wild;
                }
            }
        }
        return picked;
    }

    public static bool MustAttendSystemCombat(MemberState m, string? battlefieldSystemId)
    {
        if (battlefieldSystemId == null)
        {
            return false;
        }
        if (!battlefieldSystemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal)
            && !battlefieldSystemId.Equals(m.currentSolarSystemId, StringComparison.Ordinal))
        {
            return false;
        }
        return MemberDispatchService.TaskGuard.Equals(m.assignedTask, StringComparison.Ordinal)
            || MemberDispatchService.TaskAmbush.Equals(m.assignedTask, StringComparison.Ordinal);
    }

    private static int DeployScore(MemberState m, string? battlefieldSystemId)
    {
        var score = 0;
        if (battlefieldSystemId != null)
        {
            if (battlefieldSystemId.Equals(m.opsDeploySystemId, StringComparison.Ordinal))
            {
                score += 100;
            }
            if (battlefieldSystemId.Equals(m.currentSolarSystemId, StringComparison.Ordinal))
            {
                score += 40;
            }
        }
        if (m.assignedTask != "待命")
        {
            score += 10;
        }
        if (MustAttendSystemCombat(m, battlefieldSystemId))
        {
            score += 500;
        }
        return score;
    }
}
