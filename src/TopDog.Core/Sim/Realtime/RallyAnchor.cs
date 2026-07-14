/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §8–§9
 * 本文件: RallyAnchor.cs — 集结锚点类型与步进编码
 * ══
 */

namespace TopDog.Sim.Realtime;

public enum RallyAnchorKind
{
    SystemOnly,
    SceneLanding,
    ShipPosition,
}

public sealed class RallyAnchor
{
    public RallyAnchorKind Kind;
    public string? SystemId;
    public string? EventRegionId;
    public string? BattlefieldId;
    public float X;
    public float Y;
    public float Z;
    public float LandingDistM;
}

public static class RallyStepCodec
{
    public const string PrefixGate = "G:";
    public const string PrefixWarpLanding = "WL:";
    public const string PrefixNavigate = "N:";

    public static string Gate(string bridgeId, string targetSystemId) =>
        PrefixGate + bridgeId + ":" + targetSystemId;

    public static string WarpLanding(string targetBfId, float landingDistM) =>
        PrefixWarpLanding + targetBfId + ":" + landingDistM.ToString("F1");

    public static string Navigate(float x, float y, float z) =>
        PrefixNavigate + x.ToString("F1") + "," + y.ToString("F1") + "," + z.ToString("F1");

    public static bool TryParseGate(string step, out string bridgeId, out string targetSystemId)
    {
        bridgeId = "";
        targetSystemId = "";
        if (!step.StartsWith(PrefixGate, StringComparison.Ordinal))
        {
            return false;
        }

        var body = step[PrefixGate.Length..];
        var sep = body.IndexOf(':');
        if (sep <= 0)
        {
            return false;
        }

        bridgeId = body[..sep];
        targetSystemId = body[(sep + 1)..];
        return bridgeId.Length > 0 && targetSystemId.Length > 0;
    }

    public static bool TryParseWarpLanding(string step, out string targetBfId, out float landingDistM)
    {
        targetBfId = "";
        landingDistM = 0f;
        if (!step.StartsWith(PrefixWarpLanding, StringComparison.Ordinal))
        {
            return false;
        }

        var body = step[PrefixWarpLanding.Length..];
        var sep = body.LastIndexOf(':');
        if (sep <= 0)
        {
            return false;
        }

        targetBfId = body[..sep];
        return float.TryParse(body[(sep + 1)..], out landingDistM) && targetBfId.Length > 0;
    }

    public static bool TryParseNavigate(string step, out float x, out float y, out float z)
    {
        x = y = z = 0f;
        if (!step.StartsWith(PrefixNavigate, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = step[PrefixNavigate.Length..].Split(',');
        return parts.Length == 3
            && float.TryParse(parts[0], out x)
            && float.TryParse(parts[1], out y)
            && float.TryParse(parts[2], out z);
    }
}
