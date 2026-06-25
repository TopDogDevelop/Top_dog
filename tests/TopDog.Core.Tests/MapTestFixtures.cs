using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

internal static class MapTestFixtures
{
    public static void AttachMineSystem(GameState state)
    {
        var project = new MapProject();
        project.systems.Add(new SolarSystemDef
        {
            solarSystemId = "sys_mine",
            name = "Mine Field",
            eventRegions = new List<EventRegionDef>
            {
                new()
                {
                    eventRegionId = "er_sys_mine_belt",
                    kind = "oreBelt",
                    name = "Alpha Belt",
                    primaryMineralId = ResourceIds.Inorganic,
                },
            },
        });
        state.map = new LoadedMap(project, null);
    }
}
