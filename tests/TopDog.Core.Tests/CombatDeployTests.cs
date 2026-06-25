using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatDeployTests
{
    [Test]
    public void CounterHarvestOdds_GuardLowers_AmbushRaises()
    {
        var state = new GameState
        {
            members =
            {
                new MemberState
                {
                    memberId = "g1",
                    assignedTask = MemberDispatchService.TaskGuard,
                    opsDeploySystemId = "sys_a",
                },
                new MemberState
                {
                    memberId = "a1",
                    assignedTask = MemberDispatchService.TaskAmbush,
                    opsDeploySystemId = "sys_a",
                },
            },
        };
        var pct = CounterHarvestOddsService.ComputePercent(state, "sys_a");
        Assert.That(pct, Is.EqualTo(30 - 10 + 10));
    }

    [Test]
    public void CounterHarvestOdds_ClampedToRange()
    {
        var state = new GameState();
        for (var i = 0; i < 10; i++)
        {
            state.members.Add(new MemberState
            {
                memberId = "g" + i,
                assignedTask = MemberDispatchService.TaskGuard,
                opsDeploySystemId = "sys_a",
            });
        }
        var pct = CounterHarvestOddsService.ComputePercent(state, "sys_a");
        Assert.That(pct, Is.GreaterThanOrEqualTo(CounterHarvestOddsService.MinPercent));
        Assert.That(pct, Is.LessThanOrEqualTo(CounterHarvestOddsService.MaxPercent));
    }

    [Test]
    public void OpsDeploymentHelper_GuardInSystem_IsMandatory()
    {
        var guard = new MemberState
        {
            memberId = "g1",
            assignedTask = MemberDispatchService.TaskGuard,
            opsDeploySystemId = "sys_a",
        };
        Assert.That(OpsDeploymentHelper.MustAttendSystemCombat(guard, "sys_a"), Is.True);
        Assert.That(OpsDeploymentHelper.MustAttendSystemCombat(guard, "sys_b"), Is.False);
    }

    [Test]
    public void OpsDeploymentHelper_PickAlwaysIncludesGuardInSystem()
    {
        var state = new GameState
        {
            members =
            {
                new MemberState { memberId = "g1", assignedTask = MemberDispatchService.TaskGuard, opsDeploySystemId = "sys_a" },
                new MemberState { memberId = "m2", assignedTask = "待命", opsDeploySystemId = "sys_z" },
                new MemberState { memberId = "m3", assignedTask = "待命", opsDeploySystemId = "sys_z" },
            },
        };
        var picked = OpsDeploymentHelper.PickEncounterParticipants(state, "sys_a", 1, new Random(1));
        Assert.That(picked.Any(m => m.memberId == "g1"), Is.True);
    }
}
