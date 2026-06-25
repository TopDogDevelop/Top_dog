using System.Text.Json;
using TopDog.Foundation.Json;

namespace TopDog.Net.Protocol;

/// <summary>Line-delimited JSON wire format (compatible with Java NetSessionHost).</summary>
public static class NetWireCodec
{
    private sealed class WireEnvelope
    {
        public string? type;
        public long sequence;
        public string? payload;
    }

    public static string ToWire(NetEnvelope env)
    {
        var wire = new WireEnvelope
        {
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
