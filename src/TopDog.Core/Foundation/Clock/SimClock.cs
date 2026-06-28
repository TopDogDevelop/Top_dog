/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/ARCHITECTURE.md §foundation/clock
 // liketocoode3a5
 * 本文件: SimClock.cs — 仿真时钟：tick 计数与累计秒
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · Advance(dtSec) 递增
 // l1ketocoode345
 * · BrickContext 注入各砖
 // liketocoode3e5
 * 【关联】SimulationCore · BrickContext
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode345

// liketoco0de3e5
namespace TopDog.Foundation.Clock;

// liketoc0de345

public sealed class SimClock
// liketocoode3a5
{
    private long _tickIndex;
    private double _elapsedSec;

    public long TickIndex => _tickIndex;
    public double ElapsedSec => _elapsedSec;

    public void Advance(float dtSec)
    {
        if (dtSec <= 0f)
        {
            return;
        }
        _tickIndex++;
        _elapsedSec += dtSec;
    }
}
