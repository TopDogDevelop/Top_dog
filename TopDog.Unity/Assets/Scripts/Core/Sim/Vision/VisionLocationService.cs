using System.Linq;
using TopDog.AgentDiag;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §2 · docs/LEGION_SKIRMISH.md §4 · docs/VISION.md
 * 本文件: VisionLocationService.cs — 可观察战场数据源与锚点团员判定
 * 【机制要点】
 * · ListBattlefieldVisionGroups：遍历战场 → 锚点团员 → 空战场隐藏；实时交战场可正常观察/切换
 * · ListDescentEntries：groups 扁平化（兼容旧调用）
 * · IsVisionEligibleMember：trait_direct_possess | trait_intel_officer（含 identity 兜底）
 * · IsHostileRealtimeCombatBattlefield：存活敌舰 → 非「可切换」列表
 * 【实现逻辑】
 * · 每战场：扫 units + tacticalWarpInTransit（仅 toBattlefieldId）→ TryAddUnitEntry
 * · TryAddUnitEntry：友方、非 proxy、VisionGate.IsRailEligibleFriendly、团员有锚点词条
 * · ExplainEmptyDescentList：区分非实时战 / 无词条 / 未上场 / 无可用单位
 * 【关联】VisionGate · TacticalRightRail · PossessionService · SkirmishRosterValidation
 * ══
 */

namespace TopDog.Sim.Vision;



/// <summary>右栏「可观察战场」：按战场分组罗列锚点团员（可附身/情报员）。</summary>

public static class VisionLocationService

{

    public const string TraitPossess = PossessionTraits.TraitId;

    public const string TraitTacticalLink = "trait_intel_officer";



    public static bool IsVisionEligibleMember(MemberState member, GameState? state = null) =>

        HasPossessTrait(member, state) || HasTacticalLinkTrait(member, state);

    /// <summary>可观察战场罗列全部带词条的本地团员；视野锚点能力仍由 IsVisionEligibleMember 单独判定。</summary>
    public static bool IsObservableRosterMember(MemberState member, GameState? state = null)
    {
        if (member.traitIds.Count > 0)
        {
            return true;
        }
        if (state == null)
        {
            return false;
        }
        var code = Member.IdentityCodes.Of(member);
        return !string.IsNullOrWhiteSpace(code)
               && state.identities.TryGetValue(code, out var identity)
               && identity.traitIds.Count > 0;
    }



    public static bool HasPossessTrait(MemberState member, GameState? state = null) =>

        PossessionTraits.MemberHasTrait(member)

        || MemberHasTraitFromIdentity(member, state, TraitPossess);



    public static bool HasTacticalLinkTrait(MemberState member, GameState? state = null) =>

        member.traitIds.Contains(TraitTacticalLink)

        || MemberHasTraitFromIdentity(member, state, TraitTacticalLink);



    private static bool MemberHasTraitFromIdentity(MemberState member, GameState? state, string traitId)

    {

        if (state == null)

        {

            return false;

        }



        var code = Member.IdentityCodes.Of(member);

        if (string.IsNullOrWhiteSpace(code)

            || !state.identities.TryGetValue(code, out var identity))

        {

            return false;

        }



        return identity.traitIds.Contains(traitId);

    }



    public static List<VisionDescentEntry> ListDescentEntries(GameState state)
    {
        var list = new List<VisionDescentEntry>();
        foreach (var group in ListBattlefieldVisionGroups(state))
        {
            list.AddRange(group.Characters);
        }

        list.Sort((a, b) =>
        {
            var loc = string.Compare(a.LocationKey, b.LocationKey, StringComparison.Ordinal);
            if (loc != 0)
            {
                return loc;
            }

            return string.Compare(a.MemberName, b.MemberName, StringComparison.Ordinal);
        });

        return list;
    }

    /// <summary>
    /// 判断战场是否仍有存活敌舰；仅供战斗状态诊断，不用于阻止观察或切换。
    /// </summary>
    public static bool IsHostileRealtimeCombatBattlefield(BattlefieldState? bf)
    {
        if (bf == null)
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.IsDestroyed()
                || !u.Arrived(bf.timeSec)
                || u.isBuilding
                || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            if (u.side == UnitSide.ENEMY)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>可观察战场：遍历战场，仅展示含锚点团员的战场（空战场隐藏）。</summary>
    public static List<BattlefieldVisionGroup> ListBattlefieldVisionGroups(GameState state)
    {
        var groupMap = new Dictionary<string, (BattlefieldState Bf, List<VisionDescentEntry> Chars)>(
            StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var bf in state.battlefields)
        {
            if (bf.finished || bf.battlefieldId == null)
            {
                continue;
            }

            // 实时交战（有存活敌舰）不进可切换列表 — 无法切换视角进入
            if (IsHostileRealtimeCombatBattlefield(bf))
            {
                continue;
            }

            groupMap[bf.battlefieldId] = (bf, new List<VisionDescentEntry>());

            foreach (var u in bf.units)
            {
                TryAddUnitEntry(state, groupMap[bf.battlefieldId].Chars, seen, bf, u, inTransit: false);
            }

            foreach (var transit in state.tacticalWarpInTransit)
            {
                if (transit.unit.memberId == null || transit.unit.IsDestroyed())
                {
                    continue;
                }

                if (!bf.battlefieldId.Equals(transit.toBattlefieldId, StringComparison.Ordinal))
                {
                    continue;
                }

                TryAddUnitEntry(state, groupMap[bf.battlefieldId].Chars, seen, bf, transit.unit, inTransit: true);
            }
        }

        AppendLocalRosterAnchorEntries(state, groupMap, seen, out var rosterFallbackAdded);

        var groups = new List<BattlefieldVisionGroup>();
        foreach (var kv in groupMap)
        {
            if (kv.Value.Chars.Count == 0)
            {
                continue;
            }

            var bf = kv.Value.Bf;
            kv.Value.Chars.Sort((a, b) => string.Compare(a.MemberName, b.MemberName, StringComparison.Ordinal));
            var parts = ResolveLocationParts(state, bf);
            groups.Add(new BattlefieldVisionGroup
            {
                BattlefieldId = bf.battlefieldId!,
                LocationKey = BuildLocationKey(state, bf),
                ConstellationName = parts.Constellation,
                SystemName = parts.System,
                SceneName = parts.Scene,
                Characters = kv.Value.Chars,
            });
        }

        groups.Sort((a, b) => string.Compare(a.LocationKey, b.LocationKey, StringComparison.Ordinal));

        if (groups.Count == 0)
        {
            var forced = ForceBuildLocalRosterGroups(state);
            if (forced.Count > 0)
            {
                groups = forced;
                rosterFallbackAdded = groups.Sum(g => g.Characters.Count);
            }
        }

        var eligibleCount = 0;
        var localEligibleCount = 0;
        foreach (var member in state.members)
        {
            if (member.memberId == null)
            {
                continue;
            }

            if (IsObservableRosterMember(member, state))
            {
                eligibleCount++;
                if (IsLocalLegionMember(state, member))
                {
                    localEligibleCount++;
                }
            }
        }

        AgentSessionDebugLog.Write(
            "H1-H2",
            "VisionLocationService.ListBattlefieldVisionGroups",
            "result",
            new
            {
                combatRealtime = state.combatRealtimeActive,
                battlefields = state.battlefields.Count,
                members = state.members.Count,
                eligibleCount,
                localEligibleCount,
                groupCount = groups.Count,
                charCount = groups.Sum(g => g.Characters.Count),
                rosterFallbackAdded,
                activeBattlefieldId = state.activeBattlefieldId,
                explain = groups.Count == 0 ? ExplainEmptyDescentList(state) : "",
            });

        return groups;
    }

    private static void AppendLocalRosterAnchorEntries(
        GameState state,
        Dictionary<string, (BattlefieldState Bf, List<VisionDescentEntry> Chars)> groupMap,
        HashSet<string> seen,
        out int added)
    {
        added = 0;
        foreach (var member in state.members)
        {
            if (member.memberId == null || !IsObservableRosterMember(member, state))
            {
                continue;
            }

            if (!IsLocalLegionMember(state, member))
            {
                continue;
            }

            BattlefieldState? memberBf = null;
            BattlefieldUnit? memberUnit = null;
            var inTransitOnly = false;
            if (IsMemberInAuTransit(state, member.memberId))
            {
                var transit = FindTransitEntry(state, member.memberId);
                if (transit?.toBattlefieldId == null)
                {
                    continue;
                }

                memberBf = FindBattlefieldById(state, transit.toBattlefieldId);
                memberUnit = transit.unit;
                inTransitOnly = true;
            }
            else
            {
                foreach (var bf in state.battlefields)
                {
                    if (bf.finished || bf.battlefieldId == null)
                    {
                        continue;
                    }

                    foreach (var u in bf.units)
                    {
                        if (!member.memberId.Equals(u.memberId, StringComparison.Ordinal)
                            || u.side != UnitSide.FRIENDLY
                            || u.IsDestroyed()
                            || BattlefieldSceneProxyService.IsSceneProxy(u))
                        {
                            continue;
                        }

                        if (!VisionGate.IsRailEligibleFriendly(u, bf.timeSec))
                        {
                            continue;
                        }

                        memberBf = bf;
                        memberUnit = u;
                        break;
                    }

                    if (memberBf != null)
                    {
                        break;
                    }
                }
            }

            if (!inTransitOnly)
            {
                memberBf ??= ResolveMemberDesignatedBattlefield(state, member);
                memberBf ??= ResolveLegionFortressBattlefield(state, member.legionId);
                memberBf ??= ResolveLegionSpawnBattlefield(state, member.legionId);
                memberBf ??= ResolveActiveBattlefield(state);
                memberBf ??= ResolveAnyOpenBattlefield(state);
            }
            if (memberBf?.battlefieldId == null || memberBf.finished)
            {
                continue;
            }

            if (IsHostileRealtimeCombatBattlefield(memberBf))
            {
                continue;
            }

            if (!groupMap.TryGetValue(memberBf.battlefieldId, out var bucket))
            {
                groupMap[memberBf.battlefieldId] = (memberBf, new List<VisionDescentEntry>());
                bucket = groupMap[memberBf.battlefieldId];
            }

            var before = bucket.Chars.Count;
            var inTransit = inTransitOnly || (memberUnit == null && IsMemberInAuTransit(state, member.memberId));
            if (memberUnit != null)
            {
                TryAddUnitEntry(state, bucket.Chars, seen, memberBf, memberUnit, inTransit: false);
            }

            if (bucket.Chars.Count == before)
            {
                TryAddRosterOnlyEntry(state, bucket.Chars, seen, memberBf, member, inTransit);
            }

            if (bucket.Chars.Count > before)
            {
                added++;
            }
        }
    }

    private static List<BattlefieldVisionGroup> ForceBuildLocalRosterGroups(GameState state)
    {
        var memberBf = ResolveBattlefieldForVision(state);
        if (memberBf?.battlefieldId == null || IsHostileRealtimeCombatBattlefield(memberBf))
        {
            return new List<BattlefieldVisionGroup>();
        }

        var chars = new List<VisionDescentEntry>();
        foreach (var member in state.members)
        {
            if (member.memberId == null
                || !IsObservableRosterMember(member, state)
                || !IsLocalLegionMember(state, member))
            {
                continue;
            }

            var inTransit = IsMemberInAuTransit(state, member.memberId);
            chars.Add(new VisionDescentEntry
            {
                MemberId = member.memberId,
                MemberName = member.name ?? member.memberId ?? "?",
                BattlefieldId = memberBf.battlefieldId!,
                SystemId = memberBf.systemId,
                EventRegionId = memberBf.eventRegionId,
                SubLocation = memberBf.subLocation,
                UnitId = null,
                CanPossess = HasPossessTrait(member, state),
                CanTacticalLink = HasTacticalLinkTrait(member, state),
                InTransit = inTransit,
                LocationKey = BuildLocationKey(state, memberBf),
                ConstellationName = ResolveConstellationName(state, memberBf.systemId),
                SystemName = ResolveSystemName(state, memberBf.systemId),
                SceneName = ResolveSceneName(state, memberBf),
            });
        }

        if (chars.Count == 0)
        {
            return new List<BattlefieldVisionGroup>();
        }

        chars.Sort((a, b) => string.Compare(a.MemberName, b.MemberName, StringComparison.Ordinal));
        var parts = ResolveLocationParts(state, memberBf);
        return new List<BattlefieldVisionGroup>
        {
            new BattlefieldVisionGroup
            {
                BattlefieldId = memberBf.battlefieldId!,
                LocationKey = BuildLocationKey(state, memberBf),
                ConstellationName = parts.Constellation,
                SystemName = parts.System,
                SceneName = parts.Scene,
                Characters = chars,
            },
        };
    }

    private static BattlefieldState? ResolveAnyOpenBattlefield(GameState state)
    {
        foreach (var bf in state.battlefields)
        {
            if (!bf.finished && bf.battlefieldId != null)
            {
                return bf;
            }
        }

        return null;
    }

    private static BattlefieldState? ResolveLegionSpawnBattlefield(GameState state, string? legionId)
    {
        if (legionId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (bf.finished || bf.battlefieldId == null)
            {
                continue;
            }

            foreach (var u in bf.units)
            {
                if (u.side != UnitSide.FRIENDLY
                    || u.IsDestroyed()
                    || BattlefieldSceneProxyService.IsSceneProxy(u))
                {
                    continue;
                }

                if (legionId.Equals(u.legionId, StringComparison.Ordinal))
                {
                    return bf;
                }
            }
        }

        return null;
    }

    private static BattlefieldState? ResolveActiveBattlefield(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal) && !bf.finished)
            {
                return bf;
            }
        }

        return null;
    }

    private static bool IsActiveRealtimeBattlefield(GameState state, BattlefieldState bf) =>
        state.combatRealtimeActive
        && state.activeBattlefieldId != null
        && bf.battlefieldId != null
        && state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal);

    private static BattlefieldState? ResolveBattlefieldForVision(GameState state)
    {
        if (state.activeBattlefieldId != null)
        {
            foreach (var bf in state.battlefields)
            {
                if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
                {
                    return bf;
                }
            }
        }

        return ResolveActiveBattlefield(state) ?? ResolveAnyOpenBattlefield(state);
    }

    private static bool IsLocalLegionMember(GameState state, MemberState member)
        => LegionQuery.IsLocalMember(state, member);

    private static BattlefieldState? ResolveMemberDesignatedBattlefield(
        GameState state,
        MemberState member)
    {
        var systemId = member.opsDeploySystemId ?? member.currentSolarSystemId;
        var regionId = member.opsDeployEventRegionId;
        var subLocation = member.opsDeploySubLocation;
        BattlefieldState? systemFallback = null;
        foreach (var battlefield in state.battlefields)
        {
            if (battlefield.finished || battlefield.battlefieldId == null)
            {
                continue;
            }
            if (!string.IsNullOrWhiteSpace(systemId)
                && !systemId.Equals(battlefield.systemId, StringComparison.Ordinal))
            {
                continue;
            }
            systemFallback ??= battlefield;
            if (!string.IsNullOrWhiteSpace(regionId)
                && regionId.Equals(battlefield.eventRegionId, StringComparison.Ordinal))
            {
                return battlefield;
            }
            if (!string.IsNullOrWhiteSpace(subLocation)
                && subLocation.Equals(battlefield.subLocation, StringComparison.Ordinal))
            {
                return battlefield;
            }
        }
        return systemFallback;
    }

    private static BattlefieldState? ResolveLegionFortressBattlefield(GameState state, string? legionId)
    {
        if (legionId == null)
        {
            return null;
        }

        string? fortressRegion = null;
        foreach (var building in state.buildings)
        {
            if (building.legionId != null
                && building.legionId.Equals(legionId, StringComparison.Ordinal)
                && string.Equals(building.buildingType, BuildingService.LegionFortress, StringComparison.Ordinal))
            {
                fortressRegion = building.eventRegionId;
                break;
            }
        }

        if (fortressRegion == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (!bf.finished
                && bf.battlefieldId != null
                && fortressRegion.Equals(bf.eventRegionId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }

    private static void TryAddRosterOnlyEntry(
        GameState state,
        List<VisionDescentEntry> list,
        HashSet<string> seen,
        BattlefieldState bf,
        MemberState member,
        bool inTransit)
    {
        if (member.memberId == null)
        {
            return;
        }

        var dedupe = "member|" + member.memberId;
        if (!seen.Add(dedupe))
        {
            return;
        }

        list.Add(new VisionDescentEntry
        {
            MemberId = member.memberId,
            MemberName = member.name ?? member.memberId ?? "?",
            BattlefieldId = bf.battlefieldId!,
            SystemId = bf.systemId,
            EventRegionId = bf.eventRegionId,
            SubLocation = bf.subLocation,
            UnitId = null,
            CanPossess = HasPossessTrait(member, state),
            CanTacticalLink = HasTacticalLinkTrait(member, state),
            InTransit = inTransit,
            LocationKey = BuildLocationKey(state, bf),
            ConstellationName = ResolveConstellationName(state, bf.systemId),
            SystemName = ResolveSystemName(state, bf.systemId),
            SceneName = ResolveSceneName(state, bf),
        });
    }

    /// <summary>右栏空列表时的可读原因（区分门禁、未上场、非实时战）。</summary>

    public static string ExplainEmptyDescentList(GameState state)

    {

        if (!state.combatRealtimeActive)

        {

            return "非实时战斗，无法切换视角";

        }



        var anchorCount = 0;

        var anchorWithPresence = 0;

        foreach (var member in state.members)

        {

            if (!IsObservableRosterMember(member, state)
                || member.memberId == null
                || !IsLocalLegionMember(state, member))

            {

                continue;

            }



            anchorCount++;

            if (HasDescentPresence(state, member.memberId))

            {

                anchorWithPresence++;

            }

        }



        if (anchorCount == 0)

        {

            return "无可观察团员（本地军团暂无带词条团员）";

        }



        if (anchorWithPresence == 0)
        {
            return "无可观察团员（带词条团员暂无可用战场地点）";
        }

        return "无可切换团员（当前无可用战场单位）";

    }



    private static bool HasDescentPresence(GameState state, string memberId)

    {

        foreach (var bf in state.battlefields)

        {

            if (bf.finished || bf.battlefieldId == null)

            {

                continue;

            }



            foreach (var u in bf.units)

            {

                if (!memberId.Equals(u.memberId, StringComparison.Ordinal))

                {

                    continue;

                }



                if (u.side == UnitSide.FRIENDLY

                    && !u.IsDestroyed()

                    && !BattlefieldSceneProxyService.IsSceneProxy(u)

                    && VisionGate.IsRailEligibleFriendly(u, bf.timeSec))

                {

                    return true;

                }

            }

        }



        foreach (var transit in state.tacticalWarpInTransit)

        {

            if (memberId.Equals(transit.unit.memberId, StringComparison.Ordinal)

                && !transit.unit.IsDestroyed())

            {

                return true;

            }

        }



        return false;

    }



    private static bool IsMemberInAuTransit(GameState state, string memberId)

    {

        foreach (var transit in state.tacticalWarpInTransit)

        {

            if (memberId.Equals(transit.unit.memberId, StringComparison.Ordinal)

                && !transit.unit.IsDestroyed())

            {

                return true;

            }

        }



        return false;

    }

    private static TacticalWarpTransitEntry? FindTransitEntry(GameState state, string memberId)
    {
        foreach (var transit in state.tacticalWarpInTransit)
        {
            if (memberId.Equals(transit.unit.memberId, StringComparison.Ordinal)
                && !transit.unit.IsDestroyed())
            {
                return transit;
            }
        }

        return null;
    }

    private static BattlefieldState? FindBattlefieldById(GameState state, string battlefieldId)
    {
        foreach (var bf in state.battlefields)
        {
            if (battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }



    private static void TryAddUnitEntry(

        GameState state,

        List<VisionDescentEntry> list,

        HashSet<string> seen,

        BattlefieldState bf,

        BattlefieldUnit u,

        bool inTransit)

    {

        if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || u.memberId == null

            || BattlefieldSceneProxyService.IsSceneProxy(u))

        {

            return;

        }



        if (!inTransit && !VisionGate.IsRailEligibleFriendly(u, bf.timeSec))

        {

            return;

        }



        if (!inTransit && IsMemberInAuTransit(state, u.memberId))

        {

            return;

        }

        var member = FindMember(state, u.memberId);

        if (member == null || !IsObservableRosterMember(member, state))

        {

            return;

        }



        var dedupe = "member|" + u.memberId;

        if (!seen.Add(dedupe))

        {

            return;

        }



        list.Add(new VisionDescentEntry

        {

            MemberId = member.memberId!,

            MemberName = member.name ?? member.memberId ?? "?",

            BattlefieldId = bf.battlefieldId!,

            SystemId = bf.systemId,

            EventRegionId = bf.eventRegionId,

            SubLocation = bf.subLocation,

            UnitId = u.unitId,

            CanPossess = HasPossessTrait(member, state),

            CanTacticalLink = HasTacticalLinkTrait(member, state),

            InTransit = inTransit,

            LocationKey = BuildLocationKey(state, bf),

            ConstellationName = ResolveConstellationName(state, bf.systemId),

            SystemName = ResolveSystemName(state, bf.systemId),

            SceneName = ResolveSceneName(state, bf),

        });

    }



    public static string BuildLocationKey(GameState state, BattlefieldState bf)

    {

        var constellation = ResolveConstellationName(state, bf.systemId);

        var system = ResolveSystemName(state, bf.systemId);

        var scene = ResolveSceneName(state, bf);

        return string.Join("\u001f", constellation, system, scene);

    }



    public static (string Constellation, string System, string Scene) ResolveLocationParts(

        GameState state,

        BattlefieldState bf)

    {

        var parts = BuildLocationKey(state, bf).Split('\u001f');

        return (

            parts.Length > 0 ? parts[0] : "?",

            parts.Length > 1 ? parts[1] : "?",

            parts.Length > 2 ? parts[2] : "?");

    }



    private static string ResolveConstellationName(GameState state, string? systemId)

    {

        var sys = state.map?.Project?.FindSystem(systemId);

        if (sys?.constellationId == null || state.map?.Project?.constellations == null)

        {

            return "";

        }



        foreach (var c in state.map.Project.constellations)

        {

            if (sys.constellationId.Equals(c.constellationId, StringComparison.Ordinal))

            {

                return c.name ?? c.constellationId ?? "";

            }

        }



        return sys.constellationId;

    }



    private static string ResolveSystemName(GameState state, string? systemId)

    {

        var sys = state.map?.Project?.FindSystem(systemId);

        return sys?.name ?? systemId ?? "?";

    }



    private static string ResolveSceneName(GameState state, BattlefieldState bf)

    {

        if (bf.systemId != null && bf.eventRegionId != null)

        {

            var er = TacticalSceneBattlefieldService.FindEventRegion(state, bf.systemId, bf.eventRegionId);

            if (er?.name != null)

            {

                return er.name;

            }

        }



        return bf.subLocation ?? bf.eventRegionId ?? bf.battlefieldId ?? "?";

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



public sealed class VisionDescentEntry

{

    public string MemberId { get; init; } = "";

    public string MemberName { get; init; } = "?";

    public string BattlefieldId { get; init; } = "";

    public string? SystemId { get; init; }

    public string? EventRegionId { get; init; }

    public string? SubLocation { get; init; }

    public string? UnitId { get; init; }

    public bool CanPossess { get; init; }

    public bool CanTacticalLink { get; init; }

    public bool InTransit { get; init; }

    public string LocationKey { get; init; } = "";

    public string ConstellationName { get; init; } = "";

    public string SystemName { get; init; } = "";

    public string SceneName { get; init; } = "";

}



public sealed class BattlefieldVisionGroup

{

    public string BattlefieldId { get; init; } = "";

    public string LocationKey { get; init; } = "";

    public string ConstellationName { get; init; } = "";

    public string SystemName { get; init; } = "";

    public string SceneName { get; init; } = "";

    public List<VisionDescentEntry> Characters { get; init; } = new();

}


