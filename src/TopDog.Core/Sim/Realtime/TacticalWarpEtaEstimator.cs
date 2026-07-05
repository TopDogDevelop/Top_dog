using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>跃迁各阶段剩余时间估算（UI / 诊断）。</summary>
public static class TacticalWarpEtaEstimator
{
    public static float DistanceToLandingM(BattlefieldUnit u)
    {
        var dx = u.warpLandingX - u.x;
        var dy = u.warpLandingY - u.y;
        var dz = u.warpLandingZ - u.z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static float EstimateBurstToLandingDistM(
        float originX,
        float originY,
        float originZ,
        float ex,
        float ey,
        float ez,
        float landingDistM)
    {
        TacticalWarpLandingService.ComputeLandingPoint(
            originX, originY, originZ, ex, ey, ez, landingDistM, out var lx, out var ly, out var lz);
        var dx = lx - ex;
        var dy = ly - ey;
        var dz = lz - ez;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static float EstimateBurstToLandingDistM(float ex, float ey, float ez, float landingDistM) =>
        EstimateBurstToLandingDistM(0f, 0f, 0f, ex, ey, ez, landingDistM);

    /// <summary>对端 EntryBurst + LandingDecel 预估（设计：最长 10s + 2s；距离不足则提前到达）。</summary>
    public static float EstimatePostEntryLandingSec(float burstToLandingDistM)
    {
        var travelSec = burstToLandingDistM / TacticalWarpService.PseudoWarpSpeedMps;
        var burstSec = MathF.Min(TacticalWarpService.EntryBurstSec, travelSec);
        var afterBurstDist = MathF.Max(0f, burstToLandingDistM - burstSec * TacticalWarpService.PseudoWarpSpeedMps);
        if (afterBurstDist <= TacticalWarpService.ProxyArriveThresholdM)
        {
            return burstSec;
        }

        return burstSec + TacticalWarpService.LandingDecelSec;
    }

    public static float EstimateRemainingSec(BattlefieldUnit u, HullDef? hull = null)
    {
        switch (u.warpPhase)
        {
            case TacticalWarpPhase.PrepareInitiate:
            case TacticalWarpPhase.ApproachProxy:
                return MathF.Max(0f, TacticalWarpService.ApproachTimeoutSec - u.warpPhaseTimerSec);
            case TacticalWarpPhase.InTransit:
                return MathF.Max(0f, u.warpEtaSec);
            case TacticalWarpPhase.EntryBurst:
            {
                var dist = DistanceToLandingM(u);
                var travelSec = dist / TacticalWarpService.PseudoWarpSpeedMps;
                var burstLeft = MathF.Max(0f, TacticalWarpService.EntryBurstSec - u.warpPhaseTimerSec);
                if (travelSec <= burstLeft)
                {
                    return travelSec;
                }

                return burstLeft + TacticalWarpService.LandingDecelSec;
            }
            case TacticalWarpPhase.LandingDecel:
                return MathF.Max(0f, TacticalWarpService.LandingDecelSec - u.warpPhaseTimerSec);
            default:
                return 0f;
        }
    }

    public static string FormatRemainingLabel(BattlefieldUnit u, HullDef? hull = null)
    {
        if (u.warpPhase == TacticalWarpPhase.None && !u.inTacticalWarp)
        {
            return "";
        }

        var sec = EstimateRemainingSec(u, hull);
        var phase = u.warpPhase switch
        {
            TacticalWarpPhase.PrepareInitiate => "起跳准备",
            TacticalWarpPhase.ApproachProxy => "飞向出口",
            TacticalWarpPhase.InTransit => "在途",
            TacticalWarpPhase.EntryBurst => "入场加速",
            TacticalWarpPhase.LandingDecel => "落地减速",
            _ => "跃迁",
        };
        var dist = u.warpPhase is TacticalWarpPhase.EntryBurst or TacticalWarpPhase.LandingDecel
            ? $" · 距落点 {DistanceToLandingM(u) / 1000f:0.0}km"
            : "";
        return $" · {phase} ~{sec:0.0}s{dist}";
    }
}
