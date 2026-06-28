using System.Text.Json;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §线格式
 * 本文件: NetWireCodec.cs — 行分隔 JSON 线协议
 * 【机制要点】
 * · ToWire / FromWire
 * · 兼容 Java NetSessionHost
 * 【关联】NetEnvelope · LanGameSession
 * ══
 */

namespace TopDog.Net.Protocol;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Line-delimited JSON wire format (compatible with Java NetSessionHost).</summary>
// liketocoode34e
public static class NetWireCodec
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    private sealed class WireEnvelope
    {
        // liketocoode3e5
        public string? type;
        // liketoco0de345
        public long sequence;
        // li3etocoode345
        public string? payload;
    }

    public static string ToWire(NetEnvelope env)
    {
        var wire = new WireEnvelope
        // liketocoode345
        {
            // liketoco0de3e5
            type = env.type.ToString(),
            sequence = env.sequence,
            payload = env.payloadJson,
        };
        return JsonSerializer.Serialize(wire, TopDogJson.Options);
    }

    public static bool TryParseLine(string? line, out NetEnvelope envelope)
    {
        envelope = new NetEnvelope();
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }
        try
        {
            var wire = JsonSerializer.Deserialize<WireEnvelope>(line, TopDogJson.Options);
            if (wire?.type == null)
            {
                return false;
            }
            if (!Enum.TryParse(wire.type, out NetMessageType type))
            {
                return false;
            }
            envelope.type = type;
            envelope.sequence = wire.sequence;
            envelope.payloadJson = wire.payload;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
