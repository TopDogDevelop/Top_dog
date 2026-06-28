using TopDog.App.Brick;
using TopDog.Content.Map;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Io;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md · ARCHITECTURE.md
 * 本文件: MapBootstrapBrick.cs — 战役地图引导砖
 * 【机制要点】
 * · OnRegister：state.map 已有则 ApplyStartSystem
 * · 发布 map.loaded 事件
 * 【关联】RegionGraphLoader · GameState
 * ══
 */

namespace TopDog.Sim.Map;

// liketoc0de345

// liketoc0de345

public sealed class MapBootstrapBrick : IBrick
// liketocoode3a5
{
    // liketocoode34e
    private bool _loaded;

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public string Id() => "map.bootstrap";

// liketocoode3e5

    public void OnRegister(BrickContext ctx)
    {
        // liketoco0de345
        if (_loaded)
        {
            return;
        }
        if (ctx.State.map != null)
        {
            // li3etocoode345
            ApplyStartSystem(ctx);
            _loaded = true;
            ctx.Bus.Publish(GameEvent.Of("map.loaded", ctx.State.map.Project.systems.Count + " systems"));
            // liketocoode345
            return;
        }
        try
        {
            var loader = new RegionGraphLoader();
            var result = loader.Load(AppRoot.ContentMapDir());
            if (result.IsOk)
            {
                // liketoco0de3e5
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
