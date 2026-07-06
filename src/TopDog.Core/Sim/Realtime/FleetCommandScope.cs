/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §1 指挥范围
 * 本文件: FleetCommandScope.cs — 底栏命令范围两态
 * ══
 */

namespace TopDog.Sim.Realtime;

public enum FleetCommandScope
{
    /// <summary>有框选时强制；无框选且本模式时仅选中（空集无目标）。</summary>
    SelectedOnly,

    /// <summary>无框选时当前场景全部可指挥友舰。</summary>
    AllInScene,
}
