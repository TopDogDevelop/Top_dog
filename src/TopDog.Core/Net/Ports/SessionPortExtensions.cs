using System.Text.Json;
using TopDog.Foundation.Json;
using TopDog.Net.Protocol;
using TopDog.Sim.Realtime;

namespace TopDog.Net.Ports;

public static class SessionPortExtensions
{
    public static string SubmitCommand(this SessionPort port, string line, string? legionId = null)
    {
        port.Send(new NetEnvelope
        {
            type = NetMessageType.COMMAND_SUBMIT,
            payloadJson = CommandSubmitCodec.ToJson(line, legionId),
        });
        if (port is Local.LocalSessionHost local)
        {
            return local.ConsumeLastCommandResult() ?? "已执行";
        }
        return "已发送";
    }

    public static void SubmitTacticalInput(this SessionPort port, PossessionInputSample sample)
    {
        port.Send(new NetEnvelope
        {
            type = NetMessageType.TACTICAL_INPUT,
            sequence = sample.sequence,
            payloadJson = JsonSerializer.Serialize(sample, TopDogJson.Options),
        });
    }
}
