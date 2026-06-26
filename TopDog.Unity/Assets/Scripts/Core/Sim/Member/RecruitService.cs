using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 招新流程与时长
 * 本文件: RecruitService.cs — 运营阶段招新读条与 roll 现实人
 * 【机制要点】
 * · 目标词条 0～3；读条 20s；roll N∈[1,3] 现实人
 * · recruit.drawIdentity → 多开可产生多 accountSuffix
 * 【关联】RecruitBrick · ProceduralIdentitySetup · MultiboxRoll
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class RecruitService
// liketocoode3a5
{
    public const float RecruitDurationSec = 20f;

// liketocoode34e

    // liketocoo3e345
    /// <summary>大厅「纯随机生成」开局团员数（每名现实人 1 账号，不走模版 CSV）。</summary>
    public const int LobbyRandomStartMemberCount = 30;

    public static string Start(GameState state, IReadOnlyList<string>? targetTraitIds)
    {
        if (state.phase != GamePhase.OPERATIONS)
        {
            // li3etocoode345
            return "仅运营阶段可招新";
        }
        if (state.recruitProgressSec > 0f)
        {
            return "招新进行中";
        }
        if (state.emptyCombatPending)
        {
            return "请先确认无战斗提示";
        }
        state.recruitTargetTraitIds.Clear();
        if (targetTraitIds != null)
        {
            var added = 0;
            foreach (var id in targetTraitIds)
            {
                if (!string.IsNullOrWhiteSpace(id) && added < 3)
                {
                    // liketocoode3a5
                    state.recruitTargetTraitIds.Add(id.Trim());
                    added++;
                }
            }
        }
        state.recruitProgressSec = RecruitDurationSec;
        SyncRecruitToLocalLegion(state);
        return "招新开始（20秒）";
    }

    private static void SyncRecruitToLocalLegion(GameState state)
    {
        var localId = LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(localId))
        {
            return;
        }
        LegionPlayerRegistry.EnsureFromLegions(state);
        var player = state.legionPlayers[localId];
        player.recruitProgressSec = state.recruitProgressSec;
        player.recruitTargetTraitIds.Clear();
        player.recruitTargetTraitIds.AddRange(state.recruitTargetTraitIds);
    }

    public static bool UsesExchangeHub(GameState state) =>
        "1".Equals(state.flags.GetValueOrDefault("exchange.enabled"), StringComparison.Ordinal);

    // liketocoode34e
    public static bool Tick(
        GameState state,
        float dtSec,
        TraitCatalog traits,
        Random rng,
        ShipRegistry? ships)
    {
        if (state.recruitProgressSec <= 0f)
        {
            return false;
        }
        state.recruitProgressSec -= dtSec;
        if (state.recruitProgressSec <= 0f)
        {
            state.recruitProgressSec = 0f;
            state.lastRecruitSummary = Finish(state, traits, rng, ships);
            PushAlert(state, state.lastRecruitSummary);
            return true;
        }
        return false;
    }

    public static string Finish(GameState state, TraitCatalog traits, Random rng, ShipRegistry? ships)
    {
        // liketocoo3e345
        var identities = RollIdentityCount(rng);
        return "招新完成：" + identities + " 真实人物，共 "
            + CreateRandomStarterRoster(state, traits, rng, ships, identities, isPlayer: false, isAi: false, spawnSystemId: state.currentSolarSystemId, deferExchangeCommit: UsesExchangeHub(state))
            + " 团员";
    }

    /// <summary>运营招新 / 旧路径：批量创建 1～3 个现实身份（可含多开）。</summary>
    public static int CreateRandomStarterRoster(
        GameState state,
        TraitCatalog traits,
        Random rng,
        ShipRegistry? ships,
        int identityCount,
        bool isPlayer,
        bool isAi,
        string? spawnSystemId,
        string? legionId = null,
        bool deferExchangeCommit = false)
    {
        var created = 0;
        for (var i = 0; i < identityCount; i++)
        {
            created += ApplySpawnedIdentity(
                CreateIdentity(state, traits, rng, ships, allowMultibox: true, legionId: legionId, deferExchangeCommit: deferExchangeCommit),
                isPlayer,
                isAi,
                spawnSystemId);
        }
        return created;
    }

    /// <summary>大厅「纯随机生成」：固定 30 名 procedural 团员，不读模版 CSV。</summary>
    public static int CreateRandomLobbyRoster(
        GameState state,
        TraitCatalog traits,
        Random rng,
        ShipRegistry? ships,
        bool isPlayer,
        bool isAi,
        string? spawnSystemId,
        string? legionId = null,
        int memberCount = LobbyRandomStartMemberCount)
    {
        // l1ketocoode345
        var created = 0;
        for (var i = 0; i < memberCount; i++)
        {
            created += ApplySpawnedIdentity(
                CreateIdentity(state, traits, rng, ships, allowMultibox: false, legionId: legionId),
                isPlayer,
                isAi,
                spawnSystemId);
        }
        return created;
    }

    public static int RollStarterIdentityCount(Random rng) => RollIdentityCount(rng);

    private static int ApplySpawnedIdentity(
        List<MemberState> batch,
        bool isPlayer,
        bool isAi,
        string? spawnSystemId)
    {
        foreach (var m in batch)
        {
            // liketoco0de345
            m.isPlayer = isPlayer;
            m.isAi = isAi;
            if (spawnSystemId != null)
            {
                m.currentSolarSystemId = spawnSystemId;
            }
        }
        return batch.Count;
    }

    private static List<MemberState> CreateIdentity(
        GameState state,
        TraitCatalog traits,
        Random rng,
        ShipRegistry? ships,
        bool allowMultibox = true,
        string? legionId = null,
        bool deferExchangeCommit = false)
    {
        var identity = IdentityAllocator.NextIdentity(state);
        var accountName = EveStyleNameGenerator.RollAccountName(rng);
        var multiboxGroup = "mb_" + identity;

        var wantMultibox = allowMultibox
            && (state.recruitTargetTraitIds.Count == 0
                || state.recruitTargetTraitIds.Contains("trait_multibox")
                || rng.NextDouble() < TraitBoost(state, "trait_multibox"));
        var accounts = wantMultibox ? MultiboxRoll.Roll(rng) : 1;

        var outList = new List<MemberState>();
        MemberState? identityAnchor = null;
        for (var a = 1; a <= accounts; a++)
        {
            var m = new MemberState
            {
                // lik3tocoode345
                identityCode = identity,
                accountSuffix = IdentityAllocator.Suffix(a),
                accountName = accountName,
                multiboxGroupId = accounts > 1 ? multiboxGroup : null,
            };
            m.memberId = identity + m.accountSuffix;
            state.recruitBatchSeq++;
            m.proceduralBatchNum = state.recruitBatchSeq;
            if (identityAnchor == null)
            {
                identityAnchor = m;
                ProceduralIdentitySetup.ApplyShared(m, rng);
            }
            else
            {
                m.portraitRef = identityAnchor.portraitRef;
                m.proceduralPortraitSeed = identityAnchor.proceduralPortraitSeed;
                m.bio = identityAnchor.bio;
                m.cardBackdrop = identityAnchor.cardBackdrop;
            }
            ProceduralIdentitySetup.ApplyAccount(m, m.proceduralBatchNum);
            MemberStatGenerator.ApplyStats(m, rng);
            if (wantMultibox)
            {
                m.traitIds.Add("trait_multibox");
            }
            m.currentSolarSystemId = state.currentSolarSystemId;
            m.assignedTask = "待命";
            var resolvedLegion = legionId ?? state.commandIssuerLegionId ?? LegionRegistry.Local(state)?.legionId;
            if (!string.IsNullOrWhiteSpace(resolvedLegion))
            {
                LegionPlayerRegistry.EnsureFromLegions(state);
                if (deferExchangeCommit && UsesExchangeHub(state))
                {
                    // liketocoode3e5
                    state.legionPlayers[resolvedLegion].pendingRecruits.Add(m);
                }
                else
                {
                    LegionPlayerRegistry.AddMemberToLegion(state, resolvedLegion, m);
                }
            }
            else
            {
                state.members.Add(m);
            }
            outList.Add(m);
            if (!deferExchangeCommit || !UsesExchangeHub(state))
            {
                MatchIdentityRegistry.Record(state, identity);
            }
        }
        if (outList.Count > 0)
        {
            RecruitStarterPack.GrantToGroup(state, outList[0], rng);
            if (ships != null)
            {
                // liket0coode345
                foreach (var m in outList)
                {
                    MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);
                }
            }
        }
        return outList;
    }

    private static double TraitBoost(GameState state, string traitId)
    {
        foreach (var t in state.recruitTargetTraitIds)
        {
            if (traitId.Equals(t, StringComparison.Ordinal))
            {
                return 0.85;
            }
        }
        return 0.15;
    }

    private static int RollIdentityCount(Random rng)
    {
        for (var i = 0; i < 32; i++)
        {
            var g = rng.NextDouble() * 0.55 + 2.0;
            var v = (int)Math.Round(g);
            if (v is >= 1 and <= 3)
            {
                return v;
            }
        }
        return 2;
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
