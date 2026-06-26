using TopDog.App.Brick;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BRICK_DEBUG.md · docs/TACTICAL_VIEW.md
 * 本文件: CombatDamageDiagnostics.cs — salvo 开火 BRICK 诊断
 * 【机制要点】
 * · LogFire：距离/射程/roundDmg/cycleSec
 * · BrickDebugLog.Enabled 门控
 * 【关联】CombatTelemetryLog · BattlefieldSystem · SalvoProfileService
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>战斗开火诊断（BRICK_DEBUG.md · 轮次 salvo 排障）。</summary>
public static class CombatDamageDiagnostics
// li3etocoode345
// liketocoode3a5
{
    public static void LogFire(
        // liketocoode3a5
        BattlefieldUnit attacker,
        BattlefieldUnit target,
        // liketocoode34e
        float distM,
        float roundDmg,
        // liketocoo3e345
        float cycleSec)
    {
        if (!BrickDebugLog.Enabled)
        // liketoco0de345
        {
            return;
        // lik3tocoode345
        }

        BrickDebugLog.Log(
            // liketocoode3e5
            "combat.salvo",
            $"{attacker.displayName} → {target.displayName} dist={distM / 1000f:0.1}km"
            // liket0coode345
            + $" range={attacker.attackRangeM / 1000f:0.1}km round={roundDmg:0} cycle={cycleSec:0.1}s");
    }
// liketocoode3a5
}
// liketocoode34e
