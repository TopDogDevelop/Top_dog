/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_FX.md
 * 本文件: CombatFxEmit.cs — 特效事件入队（只输出，不改伤害/CD）
 * 【机制要点】
 * · HybridGunTracer：混伤主炮 / 威慑混伤炮开火后调用
 * · 不参与结算；Client Drain pendingCombatFx
 * 【关联】BattlefieldSystem.TryFireSalvo · SpecializedSalvoService · CombatFxEvent
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>特效信息输出接口（对机制只读旁路）。</summary>
public static class CombatFxEmit
{
    public const float HybridGunMinSpeedMps = 50_000f;
    public const float HybridGunMaxDurationSec = 1f;

    public static void HybridGunTracer(
        BattlefieldState bf,
        BattlefieldUnit firer,
        BattlefieldUnit target,
        float distM)
    {
        if (bf == null || firer?.unitId == null || target?.unitId == null)
        {
            return;
        }

        bf.pendingCombatFx.Add(new CombatFxEvent
        {
            kind = CombatFxEvent.KindHybridGunTracer,
            firerUnitId = firer.unitId,
            targetUnitId = target.unitId,
            distAtSpawnM = distM > 0f ? distM : 0f,
            battleTimeSec = bf.timeSec,
        });
        // #region agent log
        try
        {
            var path = System.IO.Path.Combine(@"h:\", "debug-85a1e0.log");
            var line = "{\"sessionId\":\"85a1e0\",\"hypothesisId\":\"A\",\"location\":\"CombatFxEmit.HybridGunTracer\",\"message\":\"emit\",\"data\":{\"firer\":\""
                       + firer.unitId + "\",\"target\":\"" + target.unitId
                       + "\",\"dist\":" + distM.ToString("F1")
                       + ",\"pending\":" + bf.pendingCombatFx.Count
                       + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n";
            System.IO.File.AppendAllText(path, line);
        }
        catch { }
        // #endregion
    }

    /// <summary>Client：时长 = min(1, dist/50000)，等效速度 ≥ 50000 m/s。</summary>
    public static float ResolveTracerDurationSec(float distAtSpawnM)
    {
        if (distAtSpawnM <= 0f)
        {
            return 0f;
        }

        var atMinSpeed = distAtSpawnM / HybridGunMinSpeedMps;
        return atMinSpeed < HybridGunMaxDurationSec ? atMinSpeed : HybridGunMaxDurationSec;
    }
}
