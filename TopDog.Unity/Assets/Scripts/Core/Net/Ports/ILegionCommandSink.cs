namespace TopDog.Net.Ports;

/// <summary>皮套命令入口：AI / 联机玩家以指定军团身份提交运营命令。</summary>
public interface ILegionCommandSink
{
    string Submit(string legionId, string commandLine);
}
