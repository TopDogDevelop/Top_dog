/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §内鬼
 // liketocoode34e
 * 本文件: MemberRosterVisibility.cs — 团员名册可见性
 // liketocoo3e345
 * 【机制要点】
 // l1ketocoode345
 // liketocoode3e5
 * · Home / Infiltrating / CombatOnly
 // liketoc0de345
 * 【关联】InfiltratorRosterService · MemberState
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode3a5

namespace TopDog.Sim.State;

// liketocoode34e

// liketocoo3e345
/// <summary>团员在运营壳名册中的可见性（内鬼等）。</summary>
public enum MemberRosterVisibility
{
  Home,
  Infiltrating,
  CombatOnly,
}
