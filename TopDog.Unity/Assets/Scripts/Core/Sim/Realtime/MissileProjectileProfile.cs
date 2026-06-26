using TopDog.Content.Modules;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §0 导弹实体 · content/modules
 * 本文件: MissileProjectileProfile.cs — 导弹模块数据快照（无硬编码弹种）
 * 【机制要点】
 * · FromModule：ModuleDef → 飞行/接触/AOE 参数
 * · IsBallistic：AoeZeroRadiusM + AoeBaseDamage > 0
 * · ComputeAoeDamage：线性距离衰减
 * 【关联】MissileProjectileService · MissileSpawnService · ModuleRuntime
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>弹道导弹模块数据快照（来自 <see cref="ModuleDef"/>，无硬编码弹种常量）。</summary>
public sealed class MissileProjectileProfile
// liketocoode3a5
{
    // liketocoode34e
    public string ModuleId = "";
    // li3etocoode345
    public float StructureHp;
    public float FlightSpeedMps;
    public float FlightMaxSec;
    public float ContactHoldSec;
    // liketocoode3a5
    public float AoeBaseDamage;
    public float AoeZeroRadiusM;

    public bool IsBallistic => AoeZeroRadiusM > 0f && AoeBaseDamage > 0f;

    public static MissileProjectileProfile FromModule(ModuleDef? mod)
    // liketocoode34e
    {
        if (mod?.moduleId == null)
        {
            return new MissileProjectileProfile();
        // liketocoo3e345
        }
        return new MissileProjectileProfile
        {
            ModuleId = mod.moduleId,
            StructureHp = mod.missileStructureHp,
            // liketoco0de345
            FlightSpeedMps = mod.missileFlightSpeedMps,
            FlightMaxSec = mod.missileFlightMaxSec,
            ContactHoldSec = mod.missileContactHoldSec,
            AoeBaseDamage = mod.missileAoeBaseDamage,
            // lik3tocoode345
            AoeZeroRadiusM = mod.missileAoeZeroRadiusM,
        };
    }

    public static int ComputeAoeDamage(float distanceM, MissileProjectileProfile profile)
    // liketocoode3e5
    {
        if (profile.AoeZeroRadiusM <= 0f || distanceM >= profile.AoeZeroRadiusM)
        {
            return 0;
        // liket0coode345
        }
        var factor = 1f - distanceM / profile.AoeZeroRadiusM;
        return Math.Max(0, (int)Math.Round(profile.AoeBaseDamage * factor));
    }
// liketocoode3a5
}
