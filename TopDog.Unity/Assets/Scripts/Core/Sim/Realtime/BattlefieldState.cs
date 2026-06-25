using TopDog.Sim.Combat;

namespace TopDog.Sim.Realtime;

public sealed class BattlefieldState
{
    public string? battlefieldId;
    public string? combatEntryId;
    public string? systemId;
    public string? solarSystemId
    {
        get => systemId;
        set => systemId = value;
    }
    public string? subLocation;
    public string? eventRegionId;
    /// <summary>战场锚点（AU）；spawn 时从地图 eventRegion 写入，用于战场间跃迁 ETA。</summary>
    public float[] anchorAu = new float[3];
    public string? targetBuildingId;
    public CombatSubtype? combatSubtype;
    public string? capturedMemberId;
    public string? harvesterMemberId;
    public bool harvesterRetreatRequested;
    public CombatResolveMode resolveMode = CombatResolveMode.REALTIME;
    public float timeSec;
    public bool finished;
    public UnitSide? winnerSide;
    public string? winReason;
    public float lastBuildingDamagedAtSec = -1f;
    public float buildingDamageAccumSec;
    public float buildingDamageThisSecond;
    public List<BattlefieldUnit> units = new();
}
