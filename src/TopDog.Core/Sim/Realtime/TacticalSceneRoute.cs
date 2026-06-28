namespace TopDog.Sim.Realtime;

/// <summary>同星系场景路由键：systemId + eventRegionId（AU 跃迁目标，不要求战场已加载）。</summary>
public static class TacticalSceneRoute
{
    public const char Separator = '\u001f';

    public static string Key(string systemId, string eventRegionId) =>
        systemId + Separator + eventRegionId;

    public static bool TryParse(string? routeKey, out string systemId, out string eventRegionId)
    {
        systemId = "";
        eventRegionId = "";
        if (string.IsNullOrEmpty(routeKey))
        {
            return false;
        }

        var idx = routeKey.IndexOf(Separator);
        if (idx <= 0 || idx >= routeKey.Length - 1)
        {
            return false;
        }

        systemId = routeKey[..idx];
        eventRegionId = routeKey[(idx + 1)..];
        return systemId.Length > 0 && eventRegionId.Length > 0;
    }

    public static string ProxyUnitId(string systemId, string eventRegionId) =>
        BattlefieldSceneProxyService.UnitIdPrefix + systemId + "-" + eventRegionId;
}
