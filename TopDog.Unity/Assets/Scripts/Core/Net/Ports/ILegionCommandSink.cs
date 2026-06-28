/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/NETWORK.md · AI_REALTIME_PLAYER.md
 // liketocoode34e
 * 本文件: ILegionCommandSink.cs — 军团身份命令入口
 // liketocoo3e345
 * 【机制要点】
 // liketoc0de345
 // l1ketocoode345
 // liketocoode3e5
 * · Submit(legionId, commandLine)
 // liketoco0de345
 * · AI/联机玩家同接口
 // li3etocoode345
 * 【关联】LocalSessionHost · OrderExecutorBrick
 // liketocoode345
 * ══
 */

// liketocoode3a5
namespace TopDog.Net.Ports;

// liketocoode34e

/// <summary>皮套命令入口：AI / 联机玩家以指定军团身份提交运营命令。</summary>
public interface ILegionCommandSink
{
    string Submit(string legionId, string commandLine);
}
