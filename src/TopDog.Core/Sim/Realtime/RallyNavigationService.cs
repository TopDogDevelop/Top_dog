using TopDog.Content.Ships;
using TopDog.AgentDiag;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §8
 * 本文件: RallyNavigationService.cs — 集结链续行 tick
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class RallyNavigationService
{
    private static long _nextDebugSnapshotMs;

    public static void ClearChain(BattlefieldUnit unit)
    {
        unit.rallyActive = false;
        unit.rallyAnchorKind = null;
        unit.rallyAnchorSystemId = null;
        unit.rallyAnchorEventRegionId = null;
        unit.rallyAnchorBfId = null;
        unit.rallyPendingSteps.Clear();
        unit.rallyLastSystemId = null;
    }

    public static void AssignChain(
        GameState state,
        BattlefieldUnit unit,
        RallyAnchor anchor,
        List<string> steps)
    {
        unit.rallyActive = steps.Count > 0;
        unit.rallyAnchorKind = anchor.Kind.ToString();
        unit.rallyAnchorSystemId = anchor.SystemId;
        unit.rallyAnchorEventRegionId = anchor.EventRegionId;
        unit.rallyAnchorBfId = anchor.BattlefieldId;
        unit.rallyAnchorX = anchor.X;
        unit.rallyAnchorY = anchor.Y;
        unit.rallyAnchorZ = anchor.Z;
        unit.rallyLandingDistM = anchor.LandingDistM;
        unit.rallyPendingSteps = new List<string>(steps);
        unit.rallyLastSystemId = RallyNavigationPlanner.FindBattlefieldForUnit(state, unit)?.systemId;
    }

    public static void Tick(GameState state, ShipRegistry ships, float dtSec)
    {
        _ = dtSec;
        foreach (var bf in state.battlefields)
        {
            foreach (var unit in bf.units)
            {
                if (!unit.rallyActive || unit.rallyPendingSteps.Count == 0 || unit.IsDestroyed())
                {
                    continue;
                }

                if (unit.inTacticalWarp && unit.warpPhase != TacticalWarpPhase.None)
                {
                    continue;
                }

                TickUnit(state, unit, bf, ships);
            }
        }

        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs >= _nextDebugSnapshotMs)
        {
            _nextDebugSnapshotMs = nowMs + 5000;
            var active = state.battlefields
                .SelectMany(battlefield => battlefield.units
                    .Where(unit => unit.rallyActive)
                    .Select(unit => new
                    {
                        unit.unitId,
                        battlefieldId = battlefield.battlefieldId,
                        battlefield.systemId,
                        unit.x,
                        unit.y,
                        unit.z,
                        aiOrder = unit.aiOrder.ToString(),
                        unit.inTacticalWarp,
                        warpPhase = unit.warpPhase.ToString(),
                        nextStep = unit.rallyPendingSteps.Count > 0 ? unit.rallyPendingSteps[0] : "",
                        remainingSteps = unit.rallyPendingSteps.Count,
                    }))
                .ToArray();
            if (active.Length > 0)
            {
                // #region agent log
                AgentSessionDebugLog.WriteDebugSession(
                    "R4-R5",
                    "RallyNavigationService.cs:Tick",
                    "active rally navigation snapshot",
                    new { active, transitCount = state.tacticalWarpInTransit.Count });
                // #endregion
            }
        }
    }

    private static void TickUnit(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState bf,
        ShipRegistry ships)
    {
        var currentBf = RallyNavigationPlanner.FindBattlefieldForUnit(state, unit) ?? bf;
        var currentSystemId = currentBf.systemId;
        var step = unit.rallyPendingSteps[0];

        if (RallyStepCodec.TryParseGate(step, out _, out var targetSystemId))
        {
            if (currentSystemId != null
                && currentSystemId.Equals(targetSystemId, StringComparison.Ordinal))
            {
                unit.rallyPendingSteps.RemoveAt(0);
                LogTransition(unit, currentBf, "gate target system reached");
                if (ShouldFinishAfterSystemEntry(unit))
                {
                    ClearChain(unit);
                    return;
                }

                ContinueOrFinish(state, unit, currentBf, ships);
            }
            else if (!unit.inTacticalWarp && unit.warpPhase == TacticalWarpPhase.None)
            {
                var applied = RallyNavigationPlanner.ApplyFirstStep(
                    state,
                    unit,
                    currentBf,
                    unit.rallyPendingSteps,
                    ships);
                if (!applied)
                {
                    LogTransition(unit, currentBf, "gate rally step failed");
                }
            }

            return;
        }

        if (RallyStepCodec.TryParseWarpLanding(step, out var targetBfId, out _))
        {
            if (currentBf.battlefieldId != null
                && currentBf.battlefieldId.Equals(targetBfId, StringComparison.Ordinal)
                && !unit.inTacticalWarp
                && unit.warpPhase == TacticalWarpPhase.None)
            {
                unit.rallyPendingSteps.RemoveAt(0);
                LogTransition(unit, currentBf, "warp target battlefield reached");
                ContinueOrFinish(state, unit, currentBf, ships);
            }

            return;
        }

        if (RallyStepCodec.TryParseNavigate(step, out var nx, out var ny, out var nz))
        {
            if (unit.aiOrder == UnitAiOrder.NAVIGATE)
            {
                var dx = unit.x - nx;
                var dy = unit.y - ny;
                var dz = unit.z - nz;
                if (dx * dx + dy * dy + dz * dz < 2500f)
                {
                    unit.rallyPendingSteps.RemoveAt(0);
                    LogTransition(unit, currentBf, "navigate target reached");
                    ClearChain(unit);
                }
            }
        }
    }

    private static bool ShouldFinishAfterSystemEntry(BattlefieldUnit unit) =>
        RallyAnchorKind.SystemOnly.ToString().Equals(unit.rallyAnchorKind, StringComparison.Ordinal);

    private static void ContinueOrFinish(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState unitBf,
        ShipRegistry ships)
    {
        if (unit.rallyPendingSteps.Count == 0)
        {
            ClearChain(unit);
            return;
        }

        var applied = RallyNavigationPlanner.ApplyFirstStep(state, unit, unitBf, unit.rallyPendingSteps, ships);
        if (!applied)
        {
            LogTransition(unit, unitBf, "next rally step failed");
        }
    }

    private static void LogTransition(BattlefieldUnit unit, BattlefieldState battlefield, string message)
    {
        // #region agent log
        AgentSessionDebugLog.WriteDebugSession(
            "R4-R5",
            "RallyNavigationService.cs:transition",
            message,
            new
            {
                unit.unitId,
                battlefieldId = battlefield.battlefieldId,
                battlefield.systemId,
                unit.x,
                unit.y,
                unit.z,
                aiOrder = unit.aiOrder.ToString(),
                unit.inTacticalWarp,
                warpPhase = unit.warpPhase.ToString(),
                remainingSteps = unit.rallyPendingSteps.ToArray(),
            });
        // #endregion
    }
}
