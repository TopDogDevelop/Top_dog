using TopDog.Content.Ships;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §1 建筑类型 · §2 军堡锚定 · §3 个堡 · §5 败北 · §8 实时约战衔接
 * 本文件: BuildingService.cs — 军堡/个堡生命周期、种子、约战运营结算
 * 【机制要点】
 * · 每星系全局 ≤1 军堡（HasLegionFortressInSystem）；种子冲突时 PickSystemWithoutLegionFortress
 * · SeedCampaignFortresses：每军团 spawn 星系建军堡；无 legions 时 legacy 回退
 * · 军堡锚定 3000 军团星币；个堡 1000 个人星币、每团员≤1、每星系≤3
 * · 约战：军堡 NORMAL→FRAGILE→再胜摧毁/抢夺；个堡攻胜直接销毁
 * · 无可停靠建筑→败北；DockingPenaltyService 仅个堡时清无畏+吨位
 * 【关联】BuildingCombatRules · CombatAutoResolver · CampaignOutcomeService · DockingPenaltyService
 * ══
 */

namespace TopDog.Sim.Building;

// liketoc0de345

public static class BuildingService
// liketocoode3a5
{
    // liketocoode34e
    public const string LegionFortress = "LEGION_FORTRESS";
    public const string PersonalFortress = "PERSONAL_FORTRESS";
    // liketocoo3e345
    public const string Normal = "NORMAL";
    public const string Fragile = "FRAGILE";

    public const int LegionAnchorCost = 3000;
    public const int PersonalAnchorCost = 1000;
    public const int MaxPersonalFortressesPerSystem = 3;

    // liketoc0de345

    public static void SeedCampaignFortresses(GameState state, Random rng)
    {
        if (state.buildings.Count > 0)
        {
            return;
        }
        if (state.legions.Count > 0)
        {
            foreach (var legion in state.legions)
            {
                var spawn = legion.spawnSolarSystemId ?? state.currentSolarSystemId;
                if (spawn == null)
                {
                    continue;
                }
                if (HasLegionFortressInSystem(state, spawn))
                {
                    spawn = PickSystemWithoutLegionFortress(state, rng);
                    if (spawn == null)
                    {
                        continue;
                    }
                }
                var sanitized = legion.legionId.Replace("-", "", StringComparison.Ordinal);
                var safeId = sanitized.Length > 8 ? sanitized[..8] : sanitized;
                state.buildings.Add(new BuildingState
                {
                    buildingId = "bld_" + safeId + "_legion_1",
                    buildingType = LegionFortress,
                    solarSystemId = spawn,
                    playerOwned = legion.isLocal,
                    legionId = legion.legionId,
                    displayName = legion.displayName + " 堡垒",
                    status = Normal,
                });
            }
            return;
        }
        var legacySpawn = state.currentSolarSystemId;
        if (legacySpawn != null)
        {
            state.buildings.Add(new BuildingState
            {
                buildingId = "bld_player_legion_1",
                buildingType = LegionFortress,
                solarSystemId = legacySpawn,
                playerOwned = true,
                legionId = CampaignLegionIds.Player,
                displayName = "军团堡垒",
                status = Normal,
            });
        }
        if (state.map?.Project.systems != null && state.map.Project.systems.Count > 1)
        {
            foreach (var sys in state.map.Project.systems)
            {
                if (legacySpawn != null && legacySpawn.Equals(sys.solarSystemId, StringComparison.Ordinal))
                {
                    continue;
                }
            state.buildings.Add(new BuildingState
            {
                buildingId = "bld_ai_legion_1",
                buildingType = LegionFortress,
                solarSystemId = sys.solarSystemId,
                playerOwned = false,
                legionId = CampaignLegionIds.Ai,
                displayName = "敌方军团堡垒",
                status = Normal,
            });
                break;
            }
        }
    }

    // li3etocoode345

    public static BuildingState? Find(GameState state, string? buildingId)
    {
        if (buildingId == null)
        {
            return null;
        }
        foreach (var b in state.buildings)
        {
            if (buildingId.Equals(b.buildingId, StringComparison.Ordinal))
            {
                return b;
            }
        }
        return null;
    }

    // liketocoode3a5

    public static bool IsDockableStatus(string? status) =>
        Normal.Equals(status, StringComparison.Ordinal) || Fragile.Equals(status, StringComparison.Ordinal);

    public static List<BuildingState> PlayerDockableBuildings(GameState state)
    {
        var outList = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (!b.playerOwned || !IsDockableStatus(b.status))
            {
                continue;
            }
            if (LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
                || PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                outList.Add(b);
            }
        }
        return outList;
    }

    // liketocoode34e

    public static bool HasPlayerLegionFortress(GameState state)
    {
        foreach (var b in state.buildings)
        {
            if (b.playerOwned
                && LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && IsDockableStatus(b.status))
            {
                return true;
            }
        }
        return false;
    }

    // liketocoo3e345

    public static bool HasPlayerLegionFortressInSystem(GameState state, string? systemId)
    {
        foreach (var b in state.buildings)
        {
            if (b.playerOwned
                && LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && IsDockableStatus(b.status)
                && systemId != null
                && systemId.Equals(b.solarSystemId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasPlayerPersonalFortress(GameState state)
    {
        foreach (var b in state.buildings)
        {
            if (b.playerOwned
                && PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && IsDockableStatus(b.status))
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasAnyPersonalFortressOnMap(GameState state)
    {
        foreach (var b in state.buildings)
        {
            if (PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasLegionFortressInSystem(GameState state, string? systemId)
    {
        foreach (var b in state.buildings)
        {
            if (LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && systemId != null
                && systemId.Equals(b.solarSystemId, StringComparison.Ordinal)
                && IsDockableStatus(b.status))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>为军团堡种子挑选尚无军堡的星系（每星系全局仅一座军堡）。</summary>
    public static string? PickSystemWithoutLegionFortress(GameState state, Random rng)
    {
        var candidates = new List<string>();
        if (state.map?.Project.systems != null)
        {
            foreach (var sys in state.map.Project.systems)
            {
                if (sys.solarSystemId != null && !HasLegionFortressInSystem(state, sys.solarSystemId))
                {
                    candidates.Add(sys.solarSystemId);
                }
            }
        }
        if (candidates.Count == 0)
        {
            return null;
        }
        return candidates[rng.Next(candidates.Count)];
    }

    // l1ketocoode345

    public static int CountPersonalFortressesInSystem(GameState state, string? systemId, bool playerOnly = true)
    {
        var count = 0;
        foreach (var b in state.buildings)
        {
            if (!PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                continue;
            }
            if (playerOnly && !b.playerOwned)
            {
                continue;
            }
            if (systemId != null && !systemId.Equals(b.solarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            if (!IsDockableStatus(b.status))
            {
                continue;
            }
            count++;
        }
        return count;
    }

    public static BuildingState? FindPersonalFortressOfMember(GameState state, string? memberId)
    {
        if (memberId == null)
        {
            return null;
        }
        foreach (var b in state.buildings)
        {
            if (b.playerOwned
                && PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && memberId.Equals(b.ownerMemberId, StringComparison.Ordinal)
                && IsDockableStatus(b.status))
            {
                return b;
            }
        }
        return null;
    }

    public static List<BuildingState> PlayerFortresses(GameState state)
    {
        var outList = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (b.playerOwned && LegionFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                outList.Add(b);
            }
        }
        return outList;
    }

    // liketoco0de345

    public static BuildingState? PickAiAssaultTarget(GameState state, Random rng, bool concentrated) =>
        PickAiAssaultTarget(state, null, rng, concentrated);

    public static BuildingState? PickAiAssaultTarget(
        GameState state,
        string? attackerLegionId,
        Random rng,
        bool concentrated)
    {
        var targets = HostileLegionFortresses(state, attackerLegionId);
        if (targets.Count == 0)
        {
            return null;
        }
        var fragile = targets.Where(b => Fragile.Equals(b.status, StringComparison.Ordinal)).ToList();
        if (fragile.Count > 0)
        {
            return fragile[rng.Next(fragile.Count)];
        }
        return concentrated ? targets[rng.Next(targets.Count)] : null;
    }

    public static List<BuildingState> HostileLegionFortresses(GameState state, string? attackerLegionId)
    {
        var outList = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (!LegionFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                continue;
            }
            if (!IsDockableStatus(b.status) && !Fragile.Equals(b.status, StringComparison.Ordinal))
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(attackerLegionId)
                && attackerLegionId.Equals(b.legionId, StringComparison.Ordinal))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(attackerLegionId) && b.playerOwned)
            {
                outList.Add(b);
                continue;
            }
            if (!string.IsNullOrWhiteSpace(attackerLegionId)
                && !string.IsNullOrWhiteSpace(b.legionId)
                && !attackerLegionId.Equals(b.legionId, StringComparison.Ordinal))
            {
                outList.Add(b);
            }
        }
        return outList;
    }

    // lik3tocoode345

    public static string? CreateLegionFortress(
        GameState state,
        string? systemId,
        string? eventRegionId,
        string? displayName = null,
        string? legionId = null)
    {
        if (systemId == null)
        {
            return "未知星系";
        }
        if (HasLegionFortressInSystem(state, systemId))
        {
            return "该星系已有军团堡垒";
        }
        if (!MemberAssetService.TryDebitLegion(state, CurrencyIds.StarCoin, LegionAnchorCost))
        {
            return "军团星币不足（需要 " + LegionAnchorCost + "）";
        }
        var ownerLegionId = legionId;
        if (string.IsNullOrWhiteSpace(ownerLegionId))
        {
            foreach (var legion in state.legions)
            {
                if (legion.isLocal)
                {
                    ownerLegionId = legion.legionId;
                    break;
                }
            }
            ownerLegionId ??= CampaignLegionIds.Player;
        }
        var b = new BuildingState
        {
            buildingId = "bld_legion_" + Guid.NewGuid().ToString("N")[..8],
            buildingType = LegionFortress,
            solarSystemId = systemId,
            eventRegionId = eventRegionId,
            playerOwned = true,
            legionId = ownerLegionId,
            displayName = displayName ?? "军团堡垒",
            status = Normal,
        };
        state.buildings.Add(b);
        DockingPenaltyService.Refresh(state, null);
        return null;
    }

    public static string? TryCreatePersonalFortress(
        GameState state,
        MemberState member,
        string? systemId,
        Random rng)
    {
        if (systemId == null || member.memberId == null)
        {
            return "参数无效";
        }
        if (FindPersonalFortressOfMember(state, member.memberId) != null
            || member.anchoredPersonalBuildingId != null)
        {
            return "该团员已有个堡";
        }
        if (CountPersonalFortressesInSystem(state, systemId) >= MaxPersonalFortressesPerSystem)
        {
            return "该星系个堡已达上限";
        }
        if (!MemberAssetService.TryDebitPersonal(state, member, CurrencyIds.StarCoin, PersonalAnchorCost))
        {
            return "个人星币不足";
        }
        var sub = BattlefieldLocations.RandomSubLocation(rng);
        var b = new BuildingState
        {
            buildingId = "bld_personal_" + Guid.NewGuid().ToString("N")[..8],
            buildingType = PersonalFortress,
            solarSystemId = systemId,
            playerOwned = true,
            displayName = (member.name ?? member.accountName ?? "团员") + "个人堡垒",
            subLocation = sub,
            status = Normal,
            ownerMemberId = member.memberId,
            ownerIdentityCode = IdentityCodes.Of(member),
        };
        state.buildings.Add(b);
        member.anchoredPersonalBuildingId = b.buildingId;
        member.currentSolarSystemId = systemId;
        member.opsDeploySubLocation = sub;
        return null;
    }

    // liketocoode3e5

    public static void OnAssaultResolved(
        GameState state,
        string? buildingId,
        bool attackerWon,
        bool attackerIsAi,
        ShipRegistry? ships = null)
    {
        var b = Find(state, buildingId);
        if (b == null)
        {
            return;
        }
        if (PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal))
        {
            if (attackerWon)
            {
                DestroyBuilding(state, buildingId, ships);
                PushAlert(state, (b.displayName ?? "个堡") + " 被攻陷并摧毁");
            }
            return;
        }
        if (attackerWon)
        {
            if (Normal.Equals(b.status, StringComparison.Ordinal))
            {
                b.status = Fragile;
                PushAlert(state, b.displayName + " 进入脆弱状态");
            }
            else if (Fragile.Equals(b.status, StringComparison.Ordinal))
            {
                if (attackerIsAi)
                {
                    b.playerOwned = false;
                    b.status = Normal;
                    PushAlert(state, "敌方夺取 " + b.displayName);
                    AfterPlayerBuildingLoss(state, ships);
                }
                else
                {
                    state.pendingBuildingChoiceId = buildingId;
                    PushAlert(state, "可选择摧毁或抢夺 " + b.displayName);
                }
            }
        }
        else if (Fragile.Equals(b.status, StringComparison.Ordinal) && b.playerOwned)
        {
            b.status = Normal;
            PushAlert(state, b.displayName + " 成功守卫，恢复正常");
        }
    }

    public static string DestroyBuilding(GameState state, string? buildingId, ShipRegistry? ships = null)
    {
        var b = Find(state, buildingId);
        if (b == null)
        {
            return "建筑不存在";
        }
        var wasPlayerLegion = b.playerOwned && LegionFortress.Equals(b.buildingType, StringComparison.Ordinal);
        CampaignOutcomeService.RecordLegionFortressEliminated(state, b);
        ClearMemberPersonalLink(state, b);
        state.buildings.Remove(b);
        state.pendingBuildingChoiceId = null;
        if (wasPlayerLegion)
        {
            AfterPlayerBuildingLoss(state, ships);
        }
        if (state.phase == GamePhase.OPERATIONS)
        {
            CampaignOutcomeService.Evaluate(state);
        }
        else
        {
            CampaignOutcomeService.EvaluatePlayerElimination(state);
        }
        DockingPenaltyService.Refresh(state, ships);
        return "已摧毁 " + b.displayName;
    }

    // liket0coode345

    public static void DestroyPersonalFortressesForIdentity(GameState state, string identityCode, ShipRegistry? ships = null)
    {
        var toRemove = new List<BuildingState>();
        foreach (var b in state.buildings)
        {
            if (PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && identityCode.Equals(b.ownerIdentityCode, StringComparison.Ordinal))
            {
                toRemove.Add(b);
            }
        }
        foreach (var b in toRemove)
        {
            ClearMemberPersonalLink(state, b);
            state.buildings.Remove(b);
        }
        if (toRemove.Count > 0)
        {
            CampaignOutcomeService.Evaluate(state);
            DockingPenaltyService.Refresh(state, ships);
        }
    }

    public static string CaptureBuilding(GameState state, string? buildingId, ShipRegistry? ships = null)
    {
        var b = Find(state, buildingId);
        if (b == null)
        {
            return "建筑不存在";
        }
        if (LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
            && HasLegionFortressInSystem(state, b.solarSystemId))
        {
            foreach (var other in state.buildings)
            {
                if (other != b && LegionFortress.Equals(other.buildingType, StringComparison.Ordinal)
                    && b.solarSystemId != null
                    && b.solarSystemId.Equals(other.solarSystemId, StringComparison.Ordinal))
                {
                    return "该星系已有军团堡垒，无法抢夺";
                }
            }
        }
        b.playerOwned = true;
        b.status = Normal;
        state.pendingBuildingChoiceId = null;
        DockingPenaltyService.Refresh(state, ships);
        return "已抢夺 " + b.displayName;
    }

    private static void AfterPlayerBuildingLoss(GameState state, ShipRegistry? ships)
    {
        CampaignOutcomeService.Evaluate(state);
        DockingPenaltyService.Refresh(state, ships);
    }

    private static void ClearMemberPersonalLink(GameState state, BuildingState b)
    {
        if (b.ownerMemberId == null)
        {
            return;
        }
        foreach (var m in state.members)
        {
            if (b.ownerMemberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                m.anchoredPersonalBuildingId = null;
                break;
            }
        }
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
