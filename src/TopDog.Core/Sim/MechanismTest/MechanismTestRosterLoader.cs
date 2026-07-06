using TopDog.Content.Starting;
using TopDog.Lobby;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md §名册
 * 本文件: MechanismTestRosterLoader.cs — 场景 JSON → members + fittings
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismTestRosterLoader
{
    public static void ApplyScenario(GameState state, MechanismTestScenarioDef scenario, Random rng)
    {
        state.members.Clear();
        state.memberFittedModules.Clear();
        state.legions.Clear();

        var memberIndex = 0;
        foreach (var legionDef in scenario.legions)
        {
            state.legions.Add(new LegionState
            {
                legionId = legionDef.legionId,
                displayName = legionDef.displayName,
                playerId = legionDef.legionId,
                isLocal = legionDef.isPlayer,
                isAiControlled = legionDef.isAiControlled,
                spawnSolarSystemId = MechanismMapGenerator.SystemId,
            });

            if (legionDef.expandTemplate != null)
            {
                ExpandTemplateMembers(state, legionDef, legionDef.expandTemplate, rng, ref memberIndex);
            }

            foreach (var memberDef in legionDef.members)
            {
                AddMember(state, legionDef.legionId, memberDef, ref memberIndex);
            }
        }
    }

    private static void ExpandTemplateMembers(
        GameState state,
        MechanismTestLegionDef legionDef,
        MechanismTestExpandDef expand,
        Random rng,
        ref int memberIndex)
    {
        var templateMembers = StartingTemplateLoader.LoadMembers(expand.templateId)
            .Where(m => expand.identityCode.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            .ToList();

        foreach (var templateMember in templateMembers)
        {
            if (expand.excludeDisplayNames.Contains(templateMember.name ?? "", StringComparer.Ordinal))
            {
                continue;
            }

            var overrideDef = expand.overrides.FirstOrDefault(o =>
                o.displayName.Equals(templateMember.name, StringComparison.Ordinal));

            var hullId = overrideDef?.hullId ?? expand.defaultHullId;
            var fitted = overrideDef?.fitted ?? expand.defaultFitted;
            if (expand.randomRepairModules.Count > 0 && overrideDef == null)
            {
                fitted = new Dictionary<string, string>(fitted, StringComparer.Ordinal);
                var modId = expand.randomRepairModules[rng.Next(expand.randomRepairModules.Count)];
                var slotKey = string.IsNullOrWhiteSpace(expand.randomRepairSlotKey)
                    ? "fn_1"
                    : expand.randomRepairSlotKey;
                fitted[slotKey] = modId;
            }

            var rowKey = SkirmishTemplateRows.RowKey(expand.templateId, templateMember);
            AddMember(state, legionDef.legionId, new MechanismTestMemberDef
            {
                memberTemplateRowId = rowKey,
                memberId = $"mt_{memberIndex++:D4}",
                displayName = templateMember.name ?? templateMember.accountName ?? "?",
                hullId = hullId,
                fitted = fitted,
            }, ref memberIndex);
        }
    }

    private static void AddMember(
        GameState state,
        string legionId,
        MechanismTestMemberDef def,
        ref int memberIndex)
    {
        if (string.IsNullOrWhiteSpace(def.memberId))
        {
            def.memberId = $"mt_{memberIndex++:D4}";
        }

        var slot = new SkirmishRosterSlot
        {
            memberTemplateId = def.memberTemplateId,
            memberTemplateRowId = def.memberTemplateRowId,
            memberId = def.memberId,
            displayName = def.displayName,
            hullId = def.hullId,
            fittedModules = def.fitted.ToDictionary(kv => kv.Key, kv => (string?)kv.Value),
        };

        var member = SkirmishRosterMemberFactory.CreateMember(slot, legionId);
        if (!string.IsNullOrWhiteSpace(def.displayName))
        {
            member.name = def.displayName;
        }

        if (!string.IsNullOrWhiteSpace(def.hullId))
        {
            member.equippedHullId = def.hullId;
        }

        state.members.Add(member);
        state.memberFittedModules[member.memberId!] = def.fitted
            .ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
    }
}
