/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §1.6 · docs/SHIP_FITTING.md §伤害
 * 本文件: CombatDamageContext.cs — ApplyDamage 管线上下文
 * 【关联】BattlefieldSystem · FieldAuraDamageRouter · DamageMitigationService
 * ══
 */

namespace TopDog.Sim.Realtime;

public struct CombatDamageContext
{
    public BattlefieldState? battlefield;
    public BattlefieldUnit target;
    public BattlefieldUnit? attacker;
    public float rawDamage;
    public float shieldDamage;
    public float armorDamage;
    public float structureDamage;
    public bool structureOnly;
    public bool isRepair;
    public bool skipMitigation;

    public float TotalLayerDamage => shieldDamage + armorDamage + structureDamage;
}
