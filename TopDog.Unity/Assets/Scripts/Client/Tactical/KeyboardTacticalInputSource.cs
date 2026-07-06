using TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §6 废止附身 WASD
 * 本文件: KeyboardTacticalInputSource.cs — 键盘战术输入（本批次不再发送附身操控）
 * ══
 */

namespace TopDog.Client.Tactical;

public sealed class KeyboardTacticalInputSource : ITacticalInputSource
{
    public bool TryPoll(out PossessionInputSample sample)
    {
        sample = default;
        return false;
    }
}
