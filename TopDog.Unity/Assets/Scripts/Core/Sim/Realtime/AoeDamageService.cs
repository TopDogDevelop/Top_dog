/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §4
 * 本文件: AoeDamageService.cs — 全游戏 AOE 唯一入口（邻域 + 探索上限 200）
 * 【机制要点】
 * · MaxTargetsExplored=200；由近及远扩圈；满则 capped
 * · 衰减：linearZeroRadius（距离线性归零）
 * · 禁止业务侧 foreach bf.units 做 AOE
 * 【关联】MissileProjectileService · BattlefieldSpatialHash · CombatHostility
 * ══
 */

using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class AoeDamageService
{
    public const int MaxTargetsExplored = 200;

    public struct AoeResult
    {
        public int HitCount;
        public float DamageTotal;
        public int Explored;
        public bool Capped;
    }

    public delegate bool AoeTargetFilter(BattlefieldUnit target, BattlefieldUnit? source);
    public delegate void AoeApply(BattlefieldUnit target, float damage, float distM);

    public static AoeResult ResolveAt(
        GameState state,
        BattlefieldState bf,
        BattlefieldSpatialHash? hash,
        float ox,
        float oy,
        float oz,
        float zeroRadiusM,
        float baseDamage,
        BattlefieldUnit? source,
        bool structureOnly,
        AoeTargetFilter? filter = null,
        ShipRegistry? ships = null,
        ModuleRegistry? modules = null,
        int maxExplore = MaxTargetsExplored)
    {
        var result = new AoeResult();
        if (zeroRadiusM <= 0f || baseDamage <= 0f || maxExplore <= 0)
        {
            return result;
        }

        var query = AoeQueryService.Query(
            bf,
            hash,
            new AoeTransform(
                new AoeVector3(ox, oy, oz),
                AoeVector3.Forward,
                AoeVector3.Up),
            AoeShape.Sphere(zeroRadiusM),
            source,
            includeSource: false,
            (target, querySource) =>
                !target.IsBallisticMissile()
                && (filter == null || filter(target, querySource)),
            maxExplore);
        result.Explored = query.Explored;
        result.Capped = query.Capped;

        foreach (var hit in query.Hits)
        {
            var target = hit.Target;
            var factor = 1f - hit.NormalizedPosition;
            var applied = Math.Max(0, (int)Math.Round(baseDamage * factor));
            if (applied <= 0)
            {
                continue;
            }

            if (structureOnly)
            {
                BattlefieldSystem.ApplyStructureOnlyDamage(bf, target, applied, source);
            }
            else
            {
                BattlefieldSystem.ApplyDamage(bf, target, applied, source, state, ships, modules);
            }

            result.HitCount++;
            result.DamageTotal += applied;
        }

        return result;
    }
}
