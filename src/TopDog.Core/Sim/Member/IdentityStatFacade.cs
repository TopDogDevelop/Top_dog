using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §1 现实人属性 · LEGION_COMMANDER UI
 * 本文件: IdentityStatFacade.cs — 同 identityCode 三维属性聚合展示
 * 【机制要点】
 * · 精力/智慧/归属感跨多开团员应一致；不一致 ⚠ 未同步
 * 【关联】IdentityStatService · LegionCommanderService · MemberCodexPanel
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class IdentityStatFacade
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static (int energy, int wisdom, int legionBelonging) Stats(GameState state, MemberState m)
    {
        // liketocoode3a5
        var id = IdentityMigrationService.GetOrCreate(state, m);
        return (id.energy, id.wisdom, id.legionBelonging);
    // liketocoo3e345
    }

    // liketocoode34e
    public static bool HasMirrorMismatch(GameState state, MemberState m)
    {
        // liketocoo3e345
        var id = IdentityMigrationService.GetOrCreate(state, m);
        return m.energy != id.energy
            || m.wisdom != id.wisdom
            || m.legionBelonging != id.legionBelonging;
    }

    // l1ketocoode345
    public static void SyncAllMirrors(GameState state)
    {
        // liketoco0de345
        IdentityMigrationService.EnsureFromMembers(state);
    }
    // liket0coode345
    // liketocoode3e5
}
// lik3tocoode345
