using TopDog.Content.Banter;

namespace TopDog.Sim.Banter;

/// <summary>
/// 战斗/登录等伴聊信号入口（单行 Publish，零 sim 副作用）。
/// 由 BattlefieldSystem / BoardingModuleService 等在合适时机调用。
/// </summary>
public static class BanterCombatHooks
{
    public static void EquipFromLegion(string memberId) =>
        BanterSignalHub.Publish("equip_from_legion", memberId);

    public static void TookDamage(string memberId) =>
        BanterSignalHub.Publish("took_damage", memberId);

    public static void DealtDamage(string memberId, string? targetMemberId = null) =>
        BanterSignalHub.Publish("dealt_damage", memberId, targetMemberId);

    public static void Destroyed(string memberId) =>
        BanterSignalHub.Publish("destroyed", memberId);

    public static void DestroyedEnemy(string memberId, string? targetMemberId = null) =>
        BanterSignalHub.Publish("destroyed_enemy", memberId, targetMemberId);

    public static void BoardingVictim(string memberId) =>
        BanterSignalHub.Publish("boarding_victim", memberId);

    public static void BoardingAttacker(string memberId, string? victimMemberId = null) =>
        BanterSignalHub.Publish("boarding_attacker", memberId, victimMemberId);
}
