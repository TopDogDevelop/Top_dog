using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class AiRealtimePlayerBrain
{
    private const float RetargetIntervalSec = 30f;

    public static void Tick(GameState state, BattlefieldState bf, float dtSec)
    {
        TickSide(state, bf, UnitSide.ENEMY, dtSec);
    }

    private static void TickSide(GameState state, BattlefieldState bf, UnitSide side, float dtSec)
    {
        var possessor = PickPossessor(bf, side);
        if (possessor == null)
        {
            return;
        }

        if (!state.aiRetargetCooldownSec.TryGetValue(bf.battlefieldId ?? "", out var cd))
        {
            cd = 0f;
        }
        cd -= dtSec;
        if (cd <= 0f)
        {
            var nearest = FindNearestOpponent(bf, possessor);
            if (nearest != null)
            {
                foreach (var u in bf.units)
                {
                    if (u.side == side && !u.IsDestroyed() && !u.isBuilding)
                    {
                        u.targetUnitId = nearest.unitId;
                        u.explicitFocus = true;
                        u.aiOrder = UnitAiOrder.FOCUS;
                    }
                }
            }
            cd = RetargetIntervalSec;
        }
        state.aiRetargetCooldownSec[bf.battlefieldId ?? ""] = cd;

        FleetOrderService.RallySide(bf, side, possessor);
        foreach (var u in bf.units)
        {
            if (u.side != side || u.IsDestroyed() || u.isBuilding || ReferenceEquals(u, possessor))
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.FOLLOW;
        }

        var orbitTarget = FindNearestOpponent(bf, possessor);
        if (orbitTarget != null)
        {
            possessor.aiOrder = UnitAiOrder.ORBIT;
            possessor.orbitTargetUnitId = orbitTarget.unitId;
            possessor.targetUnitId = orbitTarget.unitId;
            possessor.explicitFocus = true;
            possessor.throttleOn = true;
        }
    }

    private static BattlefieldUnit? PickPossessor(BattlefieldState bf, UnitSide side)
    {
        BattlefieldUnit? best = null;
        var bestWeight = -1f;
        foreach (var u in bf.units)
        {
            if (u.side != side || u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.isBuilding)
            {
                continue;
            }
            var w = TonnageWeight(u.tonnageClass);
            if (w > bestWeight)
            {
                bestWeight = w;
                best = u;
            }
        }
        return best;
    }

    private static BattlefieldUnit? FindNearestOpponent(BattlefieldState bf, BattlefieldUnit self)
    {
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var other in bf.units)
        {
            if (other.side == self.side || other.IsDestroyed() || other.isBuilding || !other.Arrived(bf.timeSec))
            {
                continue;
            }
            var dx = other.x - self.x;
            var dy = other.y - self.y;
            var d = dx * dx + dy * dy;
            if (d < bestDist)
            {
                bestDist = d;
                best = other;
            }
        }
        return best;
    }

    private static float TonnageWeight(string? tonnage) => tonnage switch
    {
        "COMPLEX" => 100f,
        "SUPERCAPITAL" => 90f,
        "TITAN" => 95f,
        "CARRIER" => 80f,
        "DREADNOUGHT" => 75f,
        "BATTLESHIP" => 60f,
        "BATTLECRUISER" => 50f,
        _ => 10f,
    };
}
