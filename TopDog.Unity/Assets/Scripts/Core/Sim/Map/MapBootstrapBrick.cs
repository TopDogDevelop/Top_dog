using TopDog.App.Brick;
using TopDog.Content.Map;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Io;
using TopDog.Sim.State;

namespace TopDog.Sim.Map;

public sealed class MapBootstrapBrick : IBrick
{
    private bool _loaded;

    public string Id() => "map.bootstrap";

    public void OnRegister(BrickContext ctx)
    {
        if (_loaded)
        {
            return;
        }
        if (ctx.State.map != null)
        {
            ApplyStartSystem(ctx);
            _loaded = true;
            ctx.Bus.Publish(GameEvent.Of("map.loaded", ctx.State.map.Project.systems.Count + " systems"));
            return;
        }
        try
        {
            var loader = new RegionGraphLoader();
            var result = loader.Load(AppRoot.ContentMapDir());
            if (result.IsOk)
            {
                var map = result.Value!;
                ctx.State.map = map;
                ApplyStartSystem(ctx);
                _loaded = true;
                ctx.Bus.Publish(GameEvent.Of("map.loaded", map.Project.systems.Count + " systems"));
            }
        }
        catch (Exception e)
        {
            ctx.Bus.Publish(GameEvent.Of("map.error", e.Message));
        }
    }

    private static void ApplyStartSystem(BrickContext ctx)
    {
        var map = ctx.State.map;
        if (map == null)
        {
            return;
        }
        if (ctx.State.currentSolarSystemId == null)
        {
            var start = PickStart(map);
            if (start != null)
            {
                ctx.State.currentSolarSystemId = start.solarSystemId;
            }
        }
        foreach (var m in ctx.State.members)
        {
            if (m.currentSolarSystemId == null && ctx.State.currentSolarSystemId != null)
            {
                m.currentSolarSystemId = ctx.State.currentSolarSystemId;
            }
        }
    }

    public void Tick(BrickContext ctx, float dtSec) { }

    private static SolarSystemDef? PickStart(LoadedMap map)
    {
        foreach (var s in map.Project.systems)
        {
            if (s.solarSystemId == "sys_hub")
            {
                return s;
            }
        }
        return map.Project.systems.Count > 0 ? map.Project.systems[0] : null;
    }
}
