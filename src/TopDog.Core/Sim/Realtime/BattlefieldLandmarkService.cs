using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>战术视野固定地标（恒星等），用于屏外 bracket。</summary>
public static class BattlefieldLandmarkService
{
    public readonly struct Landmark
    {
        public readonly string Id;
        public readonly string Kind;
        public readonly string Label;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Landmark(string id, string kind, string label, float x, float y, float z)
        {
            Id = id;
            Kind = kind;
            Label = label;
            X = x;
            Y = y;
            Z = z;
        }
    }

    public static IEnumerable<Landmark> ListForBattlefield(GameState state, BattlefieldState bf)
    {
        if (string.IsNullOrWhiteSpace(bf.systemId))
        {
            yield break;
        }

        var sys = state.map?.Project?.FindSystem(bf.systemId);
        if (sys?.eventRegions == null)
        {
            yield break;
        }

        var bfAu = bf.anchorAu is { Length: >= 3 } ? bf.anchorAu : new[] { 0f, 0f, 0f };
        var foundStar = false;
        foreach (var er in sys.eventRegions)
        {
            if (!IsStarKind(er.kind))
            {
                continue;
            }

            foundStar = true;
            var starAu = er.anchorAu is { Length: >= 3 } ? er.anchorAu : new[] { 0f, 0f, 0f };
            var dx = DistanceUnits.AuToMeters(starAu[0] - bfAu[0]);
            var dy = DistanceUnits.AuToMeters(starAu[1] - bfAu[1]);
            var dz = DistanceUnits.AuToMeters(starAu[2] - bfAu[2]);
            yield return new Landmark(
                "landmark-sun-" + (er.eventRegionId ?? "star"),
                "SUN",
                er.name ?? "恒星",
                dx,
                dy,
                dz);
        }

        if (!foundStar)
        {
            yield return new Landmark(
                "landmark-sun-default",
                "SUN",
                "恒星",
                -BuildingCombatRules.AssaultStartDistanceM * 0.85f,
                0f,
                0f);
        }
    }

    private static bool IsStarKind(string? kind) =>
        "star".Equals(kind, StringComparison.OrdinalIgnoreCase)
        || "STAR".Equals(kind, StringComparison.OrdinalIgnoreCase)
        || "SUN".Equals(kind, StringComparison.OrdinalIgnoreCase);
}
