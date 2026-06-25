namespace TopDog.Sim.Combat;

public static class BattlefieldLocations
{
    private static readonly string[] SubLocations =
    {
        "小行星带", "行星轨道", "气体巨行星环", "空间站周边", "暗物质云", "彗星轨迹",
    };

    public static string RandomSubLocation(Random rng) =>
        SubLocations[rng.Next(SubLocations.Length)];
}
