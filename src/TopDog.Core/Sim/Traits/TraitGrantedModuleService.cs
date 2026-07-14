using TopDog.Content.Modules;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VIP_TRAIT_DESIGN.md · docs/SHIP_FITTING.md
 * 本文件: TraitGrantedModuleService.cs — 词条授予的隐藏模块（进局后写入舰船 fittedModules）
 * 【机制要点】
 * · fromTrait + !playerVisibleInteractable 的模块不进开战前配装
 * · 董事会召来：进战场时写入 fn_trait_admin_board → mod_admin_board_summon
 * 【关联】BoardSummonWingService · BattlefieldSpawner · ModuleDef
 * ══
 */

namespace TopDog.Sim.Traits;

public static class TraitGrantedModuleService
{
    public const string AdminBoardModuleId = "mod_admin_board_summon";
    /// <summary>虚拟功能槽：不占用舰体 fn_* 槽位表，配装 UI 不展示。</summary>
    public const string AdminBoardSlotKey = "fn_trait_admin_board";

    public static void ApplyToSpawnedUnit(GameState state, BattlefieldUnit unit, ModuleRegistry modules)
    {
        if (unit.memberId == null)
        {
            return;
        }

        var member = FindMember(state, unit.memberId);
        if (member == null)
        {
            return;
        }

        ApplyForMember(member, unit, modules);
    }

    public static void ApplyForMember(MemberState member, BattlefieldUnit unit, ModuleRegistry modules)
    {
        _ = modules;
        if (member.traitIds.Contains(TraitActiveSkillService.BoardSummonTraitId))
        {
            unit.fittedModules[AdminBoardSlotKey] = AdminBoardModuleId;
        }
        else
        {
            unit.fittedModules.Remove(AdminBoardSlotKey);
        }
    }

    private static MemberState? FindMember(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }

        return null;
    }
}
