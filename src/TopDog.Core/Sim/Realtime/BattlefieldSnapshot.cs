/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §2
 * 本文件: BattlefieldSnapshot.cs — 战斗 SoA 快照（GPU/Overview）
 * ══
 */

namespace TopDog.Sim.Realtime;

public sealed class BattlefieldSnapshot
{
    public int Count;
    public float[] X = Array.Empty<float>();
    public float[] Y = Array.Empty<float>();
    public float[] Z = Array.Empty<float>();
    public float[] Yaw = Array.Empty<float>();
    public float[] HpFrac = Array.Empty<float>();
    public int[] Faction = Array.Empty<int>();
    public string[] UnitIds = Array.Empty<string>();
    public string?[] HullIds = Array.Empty<string>();

    public void Capture(BattlefieldState bf)
    {
        var list = new List<BattlefieldUnit>(bf.units.Count);
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u) || u.IsBallisticMissile())
            {
                continue;
            }

            list.Add(u);
        }

        Count = list.Count;
        Ensure(Count);
        for (var i = 0; i < Count; i++)
        {
            var u = list[i];
            X[i] = u.x;
            Y[i] = u.y;
            Z[i] = u.z;
            Yaw[i] = u.facingRad;
            var maxHp = Math.Max(1f, u.shieldMax + u.armorMax + u.structureMax);
            var cur = Math.Max(0f, u.shieldHp + u.armorHp + u.structureHp);
            HpFrac[i] = cur / maxHp;
            Faction[i] = CombatHostility.EffectiveFactionId(u);
            UnitIds[i] = u.unitId ?? "";
            HullIds[i] = u.hullId;
        }
    }

    private void Ensure(int n)
    {
        if (X.Length >= n)
        {
            return;
        }

        X = new float[n];
        Y = new float[n];
        Z = new float[n];
        Yaw = new float[n];
        HpFrac = new float[n];
        Faction = new int[n];
        UnitIds = new string[n];
        HullIds = new string?[n];
    }
}
