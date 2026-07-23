namespace TopDog.Sim.Realtime;

public enum AoeShapeKind
{
    Sphere,
    Box,
    Cylinder,
    Cone,
    Frustum,
}

public readonly record struct AoeVector3(float X, float Y, float Z)
{
    public static readonly AoeVector3 Forward = new(0f, 0f, 1f);
    public static readonly AoeVector3 Up = new(0f, 1f, 0f);
}

public readonly record struct AoeTransform(
    AoeVector3 Origin,
    AoeVector3 Forward,
    AoeVector3 Up);

public sealed class AoeShape
{
    public AoeShapeKind Kind;
    public float RadiusM;
    public float HeightM;
    public float LengthM;
    public float HalfAngleDeg;
    public float NearRadiusM;
    public float FarRadiusM;
    public AoeVector3 HalfExtentsM;

    public static AoeShape Sphere(float radiusM) =>
        new() { Kind = AoeShapeKind.Sphere, RadiusM = radiusM };
}

public readonly record struct AoeQueryHit(
    BattlefieldUnit Target,
    float DistanceM,
    float NormalizedPosition);

public sealed class AoeQueryResult
{
    public List<AoeQueryHit> Hits { get; } = new();
    public int Explored;
    public bool Capped;
}

/// <summary>纯空间查询：形状只决定成员，不内置阵营、伤害、回复或衰减。</summary>
public static class AoeQueryService
{
    public const int DefaultMaxExplore = 200;

    public static AoeQueryResult Query(
        BattlefieldState battlefield,
        BattlefieldSpatialHash? spatialHash,
        AoeTransform transform,
        AoeShape shape,
        BattlefieldUnit? source = null,
        bool includeSource = false,
        Func<BattlefieldUnit, BattlefieldUnit?, bool>? filter = null,
        int maxExplore = DefaultMaxExplore)
    {
        var result = new AoeQueryResult();
        if (battlefield == null || shape == null || maxExplore <= 0 || !IsValid(shape))
        {
            return result;
        }

        var hash = spatialHash ?? new BattlefieldSpatialHash();
        var boundsRadius = BoundingRadius(shape);
        if (spatialHash == null)
        {
            hash.Rebuild(battlefield.units, Math.Max(boundsRadius, 5000f));
        }

        var origin = transform.Origin;
        foreach (var target in hash.QueryBounds(
                     origin.X - boundsRadius, origin.Y - boundsRadius, origin.Z - boundsRadius,
                     origin.X + boundsRadius, origin.Y + boundsRadius, origin.Z + boundsRadius,
                     maxExplore))
        {
            result.Explored++;
            result.Capped = result.Explored >= maxExplore;
            if (target.IsDestroyed()
                || (!includeSource && source != null && target.unitId == source.unitId)
                || (filter != null && !filter(target, source)))
            {
                continue;
            }

            var delta = new AoeVector3(
                target.x - origin.X,
                target.y - origin.Y,
                target.z - origin.Z);
            if (!Contains(shape, transform, delta, out var normalized))
            {
                continue;
            }

            result.Hits.Add(new AoeQueryHit(target, Length(delta), normalized));
        }

        return result;
    }

    private static bool IsValid(AoeShape shape) => shape.Kind switch
    {
        AoeShapeKind.Sphere => shape.RadiusM > 0f,
        AoeShapeKind.Box => shape.HalfExtentsM.X >= 0f
                            && shape.HalfExtentsM.Y >= 0f
                            && shape.HalfExtentsM.Z >= 0f,
        AoeShapeKind.Cylinder => shape.RadiusM > 0f && shape.HeightM > 0f,
        AoeShapeKind.Cone => shape.LengthM > 0f && shape.HalfAngleDeg > 0f && shape.HalfAngleDeg < 90f,
        AoeShapeKind.Frustum => shape.LengthM > 0f && shape.NearRadiusM >= 0f && shape.FarRadiusM >= 0f,
        _ => false,
    };

    private static float BoundingRadius(AoeShape shape) => shape.Kind switch
    {
        AoeShapeKind.Sphere => shape.RadiusM,
        AoeShapeKind.Box => Length(shape.HalfExtentsM),
        AoeShapeKind.Cylinder => MathF.Sqrt(
            shape.RadiusM * shape.RadiusM + shape.HeightM * shape.HeightM * 0.25f),
        AoeShapeKind.Cone => MathF.Sqrt(
            shape.LengthM * shape.LengthM
            + MathF.Pow(shape.LengthM * MathF.Tan(shape.HalfAngleDeg * MathF.PI / 180f), 2f)),
        AoeShapeKind.Frustum => MathF.Sqrt(
            shape.LengthM * shape.LengthM
            + MathF.Max(shape.NearRadiusM, shape.FarRadiusM)
            * MathF.Max(shape.NearRadiusM, shape.FarRadiusM)),
        _ => 0f,
    };

    private static bool Contains(
        AoeShape shape,
        AoeTransform transform,
        AoeVector3 delta,
        out float normalized)
    {
        var distance = Length(delta);
        normalized = 0f;
        if (shape.Kind == AoeShapeKind.Sphere)
        {
            normalized = Clamp01(distance / shape.RadiusM);
            return distance <= shape.RadiusM;
        }

        BuildBasis(transform, out var right, out var up, out var forward);
        var lx = Dot(delta, right);
        var ly = Dot(delta, up);
        var lz = Dot(delta, forward);
        switch (shape.Kind)
        {
            case AoeShapeKind.Box:
                normalized = Clamp01(MathF.Max(
                    SafeRatio(MathF.Abs(lx), shape.HalfExtentsM.X),
                    MathF.Max(
                        SafeRatio(MathF.Abs(ly), shape.HalfExtentsM.Y),
                        SafeRatio(MathF.Abs(lz), shape.HalfExtentsM.Z))));
                return MathF.Abs(lx) <= shape.HalfExtentsM.X
                       && MathF.Abs(ly) <= shape.HalfExtentsM.Y
                       && MathF.Abs(lz) <= shape.HalfExtentsM.Z;
            case AoeShapeKind.Cylinder:
                var halfHeight = shape.HeightM * 0.5f;
                var radial = MathF.Sqrt(lx * lx + ly * ly);
                normalized = Clamp01(MathF.Max(radial / shape.RadiusM, MathF.Abs(lz) / halfHeight));
                return radial <= shape.RadiusM && MathF.Abs(lz) <= halfHeight;
            case AoeShapeKind.Cone:
                if (lz < 0f || lz > shape.LengthM)
                {
                    return false;
                }
                var coneRadius = lz * MathF.Tan(shape.HalfAngleDeg * MathF.PI / 180f);
                var coneRadial = MathF.Sqrt(lx * lx + ly * ly);
                normalized = Clamp01(MathF.Max(lz / shape.LengthM, SafeRatio(coneRadial, coneRadius)));
                return coneRadial <= coneRadius;
            case AoeShapeKind.Frustum:
                if (lz < 0f || lz > shape.LengthM)
                {
                    return false;
                }
                var t = lz / shape.LengthM;
                var frustumRadius = shape.NearRadiusM + (shape.FarRadiusM - shape.NearRadiusM) * t;
                var frustumRadial = MathF.Sqrt(lx * lx + ly * ly);
                normalized = Clamp01(MathF.Max(t, SafeRatio(frustumRadial, frustumRadius)));
                return frustumRadial <= frustumRadius;
            default:
                return false;
        }
    }

    private static void BuildBasis(
        AoeTransform transform,
        out AoeVector3 right,
        out AoeVector3 up,
        out AoeVector3 forward)
    {
        forward = Normalize(transform.Forward, AoeVector3.Forward);
        var requestedUp = Normalize(transform.Up, AoeVector3.Up);
        right = Normalize(Cross(requestedUp, forward), new AoeVector3(1f, 0f, 0f));
        up = Normalize(Cross(forward, right), AoeVector3.Up);
    }

    private static AoeVector3 Normalize(AoeVector3 value, AoeVector3 fallback)
    {
        var length = Length(value);
        return length <= 0.0001f
            ? fallback
            : new AoeVector3(value.X / length, value.Y / length, value.Z / length);
    }

    private static AoeVector3 Cross(AoeVector3 a, AoeVector3 b) =>
        new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    private static float Dot(AoeVector3 a, AoeVector3 b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static float Length(AoeVector3 value) =>
        MathF.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);

    private static float SafeRatio(float value, float denominator) =>
        denominator <= 0.0001f ? (value <= 0.0001f ? 0f : float.PositiveInfinity) : value / denominator;

    private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
}
