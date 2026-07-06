namespace TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_DIAGNOSTICS.md §3
 * 本文件: CombatHpDeltaQueue.cs — 飘字入队与 telemetry 同步
 * 【机制要点】
 * · Enqueue → pendingHpDeltas + CombatTelemetryLog.LogHpDelta
 * · 盾/甲/结构分层 Δ；与 CombatFloatingTextPresenter 1:1
 * 【关联】BattlefieldSystem · RemoteRepairSalvoService · CombatHpDeltaEvent
 * ══
 */

/// <summary>飘字事件入队 + <see cref="CombatTelemetryLog"/> 同步（盾/甲/结构分层）。</summary>
public static class CombatHpDeltaQueue
{
    public static void Enqueue(
        BattlefieldState bf,
        BattlefieldUnit target,
        float shieldDelta,
        float armorDelta,
        float structureDelta,
        bool isHeal)
    {
        if (shieldDelta <= 0f && armorDelta <= 0f && structureDelta <= 0f)
        {
            return;
        }

        var ev = new CombatHpDeltaEvent
        {
            targetUnitId = target.unitId,
            worldX = target.x,
            worldY = target.y,
            worldZ = target.z,
            shieldDelta = shieldDelta,
            armorDelta = armorDelta,
            structureDelta = structureDelta,
            isHeal = isHeal,
            isBuilding = target.isBuilding,
            battleTimeSec = bf.timeSec,
        };
        bf.pendingHpDeltas.Add(ev);
        CombatTelemetryLog.LogHpDelta(ev);
    }
}
