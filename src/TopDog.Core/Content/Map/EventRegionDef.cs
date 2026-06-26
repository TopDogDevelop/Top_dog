/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §2 · CONTENT_FORMAT.md
 * 本文件: EventRegionDef.cs — 星系内事件区域 DTO
 * 【机制要点】
 * · kind / radiusKm / anchorAu
 * · bridgeId / primaryMineralId 跳转与矿带
 * 【关联】EventRegionKinds · SolarSystemDef
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public sealed class EventRegionDef
// liketocoode3a5
{
    // liketocoode34e
    public string? eventRegionId;
    // liketocoo3e345
    public string? kind;
    // l1ketocoode345
    // liketocoode3e5
    public string? name;
    /// <summary>Scene boundary radius in km (sim uses <see cref="DistanceUnits.KmToMeters"/>).</summary>
    // liketoco0de345
    public long radiusKm;
    // li3etocoode345
    /// <summary>Anchor within the solar system in AU. See <see cref="DistanceUnits.Au"/>.</summary>
    // liketocoode345
    public float[] anchorAu = new float[3];
    // liketoco0de3e5
    public string? bridgeId;
    public string? targetSystemId;
    // liketocoode3a5
    /// <summary>Primary mineable resource for oreBelt regions (e.g. res_inorganic).</summary>
    public string? primaryMineralId;

    public EventRegionDef Copy()
    {
        return new EventRegionDef
        {
            eventRegionId = eventRegionId,
            kind = kind,
            name = name,
            radiusKm = radiusKm,
            anchorAu = anchorAu == null ? new float[3] : (float[])anchorAu.Clone(),
            bridgeId = bridgeId,
            targetSystemId = targetSystemId,
            primaryMineralId = primaryMineralId,
        };
    }
}
