/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md · docs/FLEET_SCALE_10K.md
 * 本文件: MechanismTestOpeningState.cs — 机制测 JSON → 战场开场字段
 * 【机制要点】
 * · 各关差异只在 JSON：开场状态 / 舰队 / 地图；运行时无按 scenarioId 特判
 * ══
 */

using TopDog.Sim.Realtime;

namespace TopDog.Sim.MechanismTest;

public static class MechanismTestOpeningState
{
    public static void ApplyToBattlefield(BattlefieldState bf, MechanismTestScenarioDef scenario)
    {
        if (bf == null || scenario == null)
        {
            return;
        }

        bf.disableAutoVictory = scenario.disableAutoVictory;
        if (scenario.maxLiveMissiles > 0)
        {
            bf.maxLiveMissiles = scenario.maxLiveMissiles;
        }

        if (scenario.entityBudget > 0)
        {
            bf.entityBudget = scenario.entityBudget;
        }

        if (scenario.tickBudgetMs > 0f)
        {
            bf.tickBudgetMs = scenario.tickBudgetMs;
        }

        if (scenario.minTimeDilation > 0f)
        {
            bf.minTimeDilation = scenario.minTimeDilation;
        }
    }
}
