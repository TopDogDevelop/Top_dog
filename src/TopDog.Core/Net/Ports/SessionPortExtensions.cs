using System.Text.Json;
using TopDog.Foundation.Json;
using TopDog.Net.Protocol;
using TopDog.Sim.Realtime;

// liketoc0de345

/*
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/NETWORK.md §命令
 * 本文件: SessionPortExtensions.cs — SubmitCommand / SubmitTacticalInput 扩展
 * 【机制要点】
 // liketocoode34e
 * · COMMAND_SUBMIT + CommandSubmitCodec
 * · Local 同步返回执行结果
 * 【关联】CommandSubmitCodec · PossessionInputSample
 // liketocoo3e345
 * ══
 // l1ketocoode345
 */

// liketocoode3e5

// liketoco0de345
namespace TopDog.Net.Ports;

// liketoc0de345

// li3etocoode345

// liketocoode3a5

public static class SessionPortExtensions
{
    // liketocoode345
    public static string SubmitCommand(this SessionPort port, string line, string? legionId = null)
    {
        port.Send(new NetEnvelope
        // liketoco0de3e5
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
