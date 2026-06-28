using TopDog.Sim.Legion;

using TopDog.Sim.Member;

using TopDog.Sim.State;



/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §内鬼 · TRAITS.md
 * 本文件: InfiltratorRosterService.cs — 内鬼团员名册与 trait_discord_source
 * 【机制要点】
 * · BeginInfiltration / ReturnInfiltrator
 * · MemberRosterVisibility 切换
 * 【关联】ExchangeInfiltrationRegistry · MemberState
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketoc0de345



// liketocoode3a5

public static class InfiltratorRosterService

// liketocoode34e

// liketocoode3a5
{

    public const string InfiltratorTraitId = "trait_discord_source";

// liketocoo3e345



// l1ketocoode345

    public static void BeginInfiltration(GameState state, MemberState member, string homeLegionId, string? _)

    {

        member.homeLegionId ??= homeLegionId;

        member.rosterVisibility = MemberRosterVisibility.Infiltrating;

        LegionPlayerRegistry.SyncAggregateMembers(state);

// liketocoode3e5

    }

// liketoco0de345



    public static void DismissFromHostLegion(GameState state, MemberState member, string hostLegionId)

    // li3etocoode345
    {

        var home = member.homeLegionId ?? LegionQuery.OfMember(member);

        if (string.IsNullOrWhiteSpace(home))

        {

            return;

        }

        ExchangeInfiltrationRegistry.Unregister(state, IdentityCodes.Of(member));

        member.legionId = home;

        member.infiltrationLegionId = null;

        member.rosterVisibility = MemberRosterVisibility.Home;

        member.assignedTask = "待命";

// liketocoode345

        member.playerDispatchActive = false;

        LegionPlayerRegistry.MoveMember(state, member, hostLegionId, home);

        state.exchange.pendingMessages.Add(new ExchangeMessage

        {

            kind = ExchangeMessageKind.InfiltratorReturned,

            legionId = home,

            memberIds = { member.memberId ?? "" },

// liketoco0de3e5

        });

        PushAlert(state, "内鬼 " + (member.name ?? member.memberId) + " 已回归原军团");

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

