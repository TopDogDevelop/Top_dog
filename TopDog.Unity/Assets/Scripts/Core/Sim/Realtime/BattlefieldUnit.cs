namespace TopDog.Sim.Realtime;

public sealed class BattlefieldUnit
{
    public string? unitId;
    /// <summary>舰载机归属母舰 unitId。</summary>
    public string? parentUnitId;
    public string? memberId;
    public string? buildingId;
    public string? displayName;
    public string? hullId;
    public string? tonnageClass;
    public UnitSide side;
    public float x;
    public float y;
    public float z;
    public float vx;
    public float vy;
    public float vz;
    public float facingRad;
    public float pitchRad;
    public bool throttleOn;
    public float maxSpeedMps = 120f;
    public float accelMps2 = 50f;
    public float yawRateRadPerSec = 1.2f;
    public float pitchRateRadPerSec = 1.0f;
    public float shieldHp;
    public float shieldMax;
    public float armorHp;
    public float armorMax;
    public float structureHp;
    public float structureMax;
    public float attackRangeM = 8000f;
    public float damagePerSec = 40f;
    public float fireCooldownSec;
    public float arrivalAtSec;
    public bool alive = true;
    public bool isBuilding;
    public bool explicitFocus;
    public string? targetUnitId;
    public string? orbitTargetUnitId;
    public string? approachTargetUnitId;
    public string? rallyPointUnitId;
    public UnitAiOrder aiOrder = UnitAiOrder.IDLE;
    public bool inTacticalWarp;
    public string? warpTargetBfId;
    public float warpEtaSec;
    /// <summary>董事会召来等：不可战术跃迁离场景。</summary>
    public bool pinnedToBattlefield;
    public Dictionary<string, string> fittedModules = new();

    public bool Arrived(float battleTimeSec) => battleTimeSec >= arrivalAtSec;

    public bool IsDestroyed() => !alive || (structureMax > 0f && structureHp <= 0f);

    public float SpeedMps()
    {
        var dx = vx;
        var dy = vy;
        var dz = vz;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
