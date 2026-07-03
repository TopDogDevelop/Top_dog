using TopDog.Content;
using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>伴聊正文动态占位：成稿前将占位符替换为具体文本（地点同轮一致；装备/舰船逐次独立掷骰）。</summary>
public static class BanterDynamicTextResolver
{
    public const string OwnedSiteToken = "（随机地点）";
    public const string ModuleToken = "（随机装备）";
    public const string HullToken = "（随机舰船）";

    private static ShipRegistry? _ships;
    private static ModuleRegistry? _modules;

    public static string Resolve(GameState state, MemberBanterRuntimeState rt, string text, Random rng)
    {
        if (string.IsNullOrEmpty(text)
            || (!text.Contains(OwnedSiteToken, StringComparison.Ordinal)
                && !text.Contains(ModuleToken, StringComparison.Ordinal)
                && !text.Contains(HullToken, StringComparison.Ordinal)))
        {
            return text;
        }

        rt.idleDynamicContext ??= new BanterIdleDynamicContext();
        var ctx = rt.idleDynamicContext;

        if (text.Contains(OwnedSiteToken, StringComparison.Ordinal))
        {
            ctx.OwnedSite ??= RollOwnedSiteName(state, rng);
            text = text.Replace(OwnedSiteToken, ctx.OwnedSite, StringComparison.Ordinal);
        }

        EnsureRegistries();
        while (text.Contains(ModuleToken, StringComparison.Ordinal))
        {
            var rolled = BanterFlavorPools.RollModuleName(state, _modules!, rng);
            text = ReplaceFirst(text, ModuleToken, rolled);
        }

        while (text.Contains(HullToken, StringComparison.Ordinal))
        {
            var rolled = BanterFlavorPools.RollHullName(state, _ships!, rng);
            text = ReplaceFirst(text, HullToken, rolled);
        }

        return text;
    }

    private static string ReplaceFirst(string text, string token, string value)
    {
        var idx = text.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0)
        {
            return text;
        }

        return text.Substring(0, idx) + value + text.Substring(idx + token.Length);
    }

    private static string RollOwnedSiteName(GameState state, Random rng)
    {
        var systems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in BuildingService.PlayerDockableBuildings(state))
        {
            if (!string.IsNullOrWhiteSpace(b.solarSystemId))
            {
                systems.Add(b.solarSystemId);
            }
        }

        if (systems.Count == 0 && !string.IsNullOrWhiteSpace(state.currentSolarSystemId))
        {
            systems.Add(state.currentSolarSystemId);
        }

        if (systems.Count == 0)
        {
            return "未知星系";
        }

        var pick = systems.ElementAt(rng.Next(systems.Count));
        return ResolveSystemName(state, pick);
    }

    private static string ResolveSystemName(GameState state, string systemId)
    {
        var systems = state.map?.Project.systems;
        if (systems != null)
        {
            foreach (var sys in systems)
            {
                if (systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
                {
                    return string.IsNullOrWhiteSpace(sys.name) ? systemId : sys.name.Trim();
                }
            }
        }

        return systemId;
    }

    private static void EnsureRegistries()
    {
        _ships ??= ShipRegistry.LoadDefault();
        _modules ??= ModuleRegistry.LoadDefault();
    }

    public static void ResetCachesForTests()
    {
        _ships = null;
        _modules = null;
    }
}
