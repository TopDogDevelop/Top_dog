using TopDog.Sim.Legion;

using TopDog.Sim.Member;

using TopDog.Sim.State;



namespace TopDog.Sim.Exchange;



public static class InfiltratorRosterService

{

    public const string InfiltratorTraitId = "trait_discord_source";



    public static void BeginInfiltration(GameState state, MemberState member, string homeLegionId, string? _)

    {

        member.homeLegionId ??= homeLegionId;

        member.rosterVisibility = MemberRosterVisibility.Infiltrating;

        LegionPlayerRegistry.SyncAggregateMembers(state);

    }



    public static void DismissFromHostLegion(GameState state, MemberState member, string hostLegionId)

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

        member.playerDispatchActive = false;

        LegionPlayerRegistry.MoveMember(state, member, hostLegionId, home);

        state.exchange.pendingMessages.Add(new ExchangeMessage

        {

            kind = ExchangeMessageKind.InfiltratorReturned,

            legionId = home,

            memberIds = { member.memberId ?? "" },

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

