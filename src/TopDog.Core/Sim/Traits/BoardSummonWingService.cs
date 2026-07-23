using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_INDEX.md · VIP_TRAIT_DESIGN.md
 * 本文件: BoardSummonWingService.cs — 董事会召来：临时发射管 + 铁棺级增援翼
 * 【机制要点】
 * · 目标舰添加 5× tube_board_temp_* → mod_board_summon_wing，立即展开
 * · 翼 HP 对齐铁棺；摧毁则移除对应发射管
 * · 管理员模块由 TraitGrantedModuleService 进局授予（非开战前配装）
 * 【关联】TraitActiveSkillService · TraitGrantedModuleService · LaunchTubeStateService
 * ══
 */

namespace TopDog.Sim.Traits;

public static class BoardSummonWingService
{
    public const int WingCount = 5;
    public const string WingTonnageClass = "BOARD_SUMMON_WING";
    public const string TempTubePrefix = "tube_board_temp_";
    public const string BoardSummonWingModuleId = "mod_board_summon_wing";

    private const float WingOffsetM = 420f;
    private const float BoardWingSalvoDmg = 4400f;
    private const float BoardWingFireCycleSec = 10f;
    private const float BoardWingMaxSpeedMps = 900f;
    private const float BoardWingAccelMps2 = 400f;
    private const float BoardWingAttackRangeM = 12_000f;

    private const float IronCoffinShield = 5000f;
    private const float IronCoffinArmor = 30_000f;
    private const float IronCoffinStructure = 10_000f;

    public static string TrySummonViaTempTubes(
        GameState state,
        BattlefieldState bf,
        MemberState caster,
        string? targetUnitId,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        _ = ships;
        if (!CasterHasAdminAuthority(state, bf, caster, modules))
        {
            return "缺少董事会召来权限（词条进局后授予管理员模块）";
        }

        var targetUnit = ResolveTargetUnit(state, bf, caster, targetUnitId);
        if (targetUnit == null)
        {
            return "找不到可增援的目标舰";
        }

        if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
        {
            return "战场单位已达上限（" + BattlefieldUnitLimits.MaxUnitsPerBattlefield + "），无法召来董事会翼";
        }

        var added = AddTempTubes(targetUnit, modules);
        if (added == 0)
        {
            return "董事会召来失败（无法添加发射管）";
        }

        var spawned = DeployTempTubes(bf, targetUnit, modules, rng);
        if (spawned == 0)
        {
            return "董事会召来失败（战场已满）";
        }

        PushAlert(state, "董事会召来：" + targetUnit.displayName + " 增援 " + spawned + " 翼");
        CombatTelemetryLog.Log("board.temp_tube", "target=" + targetUnit.unitId + " count=" + spawned);
        return "已召来董事会增援 " + spawned + " 翼";
    }

    public static void TryInjectPendingAtSpawn(
        GameState state,
        BattlefieldState bf,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (string.IsNullOrWhiteSpace(state.pendingBoardSummonCasterMemberId))
        {
            return;
        }

        var caster = FindMember(state, state.pendingBoardSummonCasterMemberId);
        if (caster == null)
        {
            ClearPending(state);
            return;
        }

        var targetId = state.pendingBoardSummonTargetUnitId;
        var result = TrySummonViaTempTubes(state, bf, caster, targetId, ships, modules, rng);
        if (result.StartsWith("已召来", StringComparison.Ordinal))
        {
            ClearPending(state);
            return;
        }

        if (BoardSummonWingService.FindCasterUnit(bf, caster.memberId) == null
            && string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        FailUnresolvedPending(state, "董事会召来未生效：" + result);
    }

    public static void FailUnresolvedPending(GameState state, string message)
    {
        if (string.IsNullOrWhiteSpace(state.pendingBoardSummonCasterMemberId))
        {
            return;
        }

        ClearPending(state);
        PushAlert(state, message);
        CombatTelemetryLog.Log("board-wing-pending", "failed: " + message);
    }

    public static BattlefieldUnit? FindCasterUnit(BattlefieldState bf, string? memberId)
    {
        if (memberId == null)
        {
            return null;
        }

        foreach (var u in bf.units)
        {
            if (u.isBuilding || u.IsTemplateCarriedUnit() || u.IsDestroyed())
            {
                continue;
            }

            if (memberId.Equals(u.memberId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    public static BattlefieldUnit? ResolveAnchorUnit(
        BattlefieldState bf,
        string? memberId,
        string? legionId) =>
        FindCasterUnit(bf, memberId);

    public static bool IsTempBoardTube(string slotKey) =>
        slotKey.StartsWith(TempTubePrefix, StringComparison.Ordinal);

    public static void RemoveTempTube(BattlefieldUnit carrier, string slotKey)
    {
        carrier.fittedModules.Remove(slotKey);
        carrier.tubeStates.Remove(slotKey);
    }

    private static int AddTempTubes(BattlefieldUnit target, ModuleRegistry modules)
    {
        _ = modules;
        var added = 0;
        for (var i = 0; i < WingCount; i++)
        {
            var key = TempTubePrefix + i;
            if (target.fittedModules.ContainsKey(key))
            {
                continue;
            }

            target.fittedModules[key] = BoardSummonWingModuleId;
            target.tubeStates[key] = LaunchTubeState.Inactive;
            added++;
        }

        return added;
    }

    private static int DeployTempTubes(
        BattlefieldState bf,
        BattlefieldUnit carrier,
        ModuleRegistry modules,
        Random rng)
    {
        var spawned = 0;
        foreach (var kv in carrier.fittedModules.ToList())
        {
            if (!IsTempBoardTube(kv.Key))
            {
                continue;
            }

            if (!BoardSummonWingModuleId.Equals(kv.Value, StringComparison.Ordinal))
            {
                continue;
            }

            if (carrier.tubeStates.TryGetValue(kv.Key, out var tubeState)
                && tubeState != LaunchTubeState.Inactive)
            {
                continue;
            }

            if (!BattlefieldUnitLimits.CanSpawnNonCrewUnit(bf))
            {
                break;
            }

            var wingIndex = spawned + 1;
            bf.units.Add(SpawnBoardSummonWing(carrier, kv.Key, wingIndex, rng));
            LaunchTubeStateService.OnWingLaunched(carrier, kv.Key);
            spawned++;
            CombatTelemetryLog.LogSpawn("board-wing", bf.units[^1].unitId!, carrier.unitId);
        }

        _ = modules;
        return spawned;
    }

    private static BattlefieldUnit SpawnBoardSummonWing(
        BattlefieldUnit carrier,
        string tubeSlotKey,
        int wingIndex,
        Random rng)
    {
        var angle = wingIndex * 1.1f + (float)rng.NextDouble() * 0.5f;
        var ox = MathF.Cos(angle) * WingOffsetM;
        var oy = MathF.Sin(angle) * WingOffsetM;
        return new BattlefieldUnit
        {
            unitId = "board-wing-" + Guid.NewGuid().ToString("N")[..8],
            parentUnitId = carrier.unitId,
            memberId = carrier.memberId,
            legionId = carrier.legionId,
            displayName = "董事会增援" + wingIndex,
            hullId = BoardSummonWingModuleId,
            tonnageClass = WingTonnageClass,
            side = carrier.side,
            arrivalAtSec = 0f,
            pinnedToBattlefield = true,
            x = carrier.x + ox,
            y = carrier.y + oy,
            z = carrier.z,
            facingRad = carrier.facingRad,
            throttleOn = false,
            maxSpeedMps = BoardWingMaxSpeedMps,
            accelMps2 = BoardWingAccelMps2,
            shieldHp = IronCoffinShield,
            shieldMax = IronCoffinShield,
            armorHp = IronCoffinArmor,
            armorMax = IronCoffinArmor,
            structureHp = IronCoffinStructure,
            structureMax = IronCoffinStructure,
            attackRangeM = BoardWingAttackRangeM,
            salvoRoundDmg = BoardWingSalvoDmg,
            fireCycleSec = BoardWingFireCycleSec,
            fireCooldownSec = 0f,
            damagePerSec = BoardWingSalvoDmg / BoardWingFireCycleSec,
            alive = true,
            fittedModules = { [tubeSlotKey] = BoardSummonWingModuleId },
        };
    }

    private static bool CasterHasAdminAuthority(
        GameState state,
        BattlefieldState bf,
        MemberState caster,
        ModuleRegistry modules)
    {
        if (caster.traitIds.Contains(TraitActiveSkillService.BoardSummonTraitId))
        {
            return true;
        }

        var casterUnit = FindCasterUnit(bf, caster.memberId);
        if (casterUnit == null)
        {
            return false;
        }

        foreach (var modId in casterUnit.fittedModules.Values)
        {
            if (TraitGrantedModuleService.AdminBoardModuleId.Equals(modId, StringComparison.Ordinal))
            {
                return true;
            }

            var mod = modules.Resolve(modId);
            if (mod != null
                && "admin_board_summon".Equals(mod.moduleKind, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static BattlefieldUnit? ResolveTargetUnit(
        GameState state,
        BattlefieldState bf,
        MemberState caster,
        string? targetUnitId)
    {
        if (!string.IsNullOrWhiteSpace(targetUnitId))
        {
            var selected = BattlefieldSystem.FindUnit(bf, targetUnitId);
            if (selected != null
                && !selected.IsDestroyed()
                && selected.side == UnitSide.FRIENDLY
                && !selected.IsTemplateCarriedUnit()
                && !selected.isBuilding)
            {
                return selected;
            }
        }

        var legionId = caster.legionId;
        var candidates = new List<BattlefieldUnit>();
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || u.isBuilding || u.IsTemplateCarriedUnit() || u.side != UnitSide.FRIENDLY)
            {
                continue;
            }

            if (legionId != null
                && u.legionId != null
                && !legionId.Equals(u.legionId, StringComparison.Ordinal))
            {
                continue;
            }

            candidates.Add(u);
        }

        if (candidates.Count == 0)
        {
            return FindCasterUnit(bf, caster.memberId);
        }

        return candidates[new Random().Next(candidates.Count)];
    }

    private static void ClearPending(GameState state)
    {
        state.pendingBoardSummonCasterMemberId = null;
        state.pendingBoardSummonIdentityCode = null;
        state.pendingBoardSummonLegionId = null;
        state.pendingBoardSummonTargetUnitId = null;
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

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
