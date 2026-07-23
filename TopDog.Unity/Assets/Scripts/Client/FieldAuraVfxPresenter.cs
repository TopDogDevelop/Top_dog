using System.Collections.Generic;
using TopDog.Client.Tactical;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §6 · OPEN_DESIGN D-VFX-01
 * 本文件: FieldAuraVfxPresenter.cs — 场域球壳实例（sim→Layer29 · Fresnel+SG 噪声 · 主可见）
 * 【关联】CombatFxCameraHost · FieldAuraVfxCatalog
 * ══
 */

namespace TopDog.Client;

/// <summary>球壳世界锚点：持有舰坐标；LateUpdate 在浮动原点移动后再贴回。</summary>
public sealed class FieldAuraWorldAnchor : MonoBehaviour
{
    public Vector3 WorldCenter;
}

public sealed class FieldAuraVfxPresenter
{
    private readonly Transform _worldRoot;
    private readonly ShipRegistry _ships;
    private readonly ModuleRegistry _modules;
    private readonly Dictionary<string, GameObject> _active = new();
    private int _lastDiagSphereCount = -1;

    public FieldAuraVfxPresenter(Transform worldRoot, ShipRegistry ships, ModuleRegistry modules)
    {
        _worldRoot = worldRoot;
        _ships = ships;
        _modules = modules;
    }

    public void Refresh(GameState? state, BattlefieldState? bf, Vector3 focusWorld)
    {
        if (!ClientGameSettings.CombatFxEnabled || state == null || bf == null || _worldRoot == null)
        {
            HideAll();
            return;
        }

        var expected = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var holder in bf.units)
        {
            if (holder.IsDestroyed() || holder.isBuilding)
            {
                continue;
            }

            if (holder.fieldAuraEnabledAtSec <= 0f
                || holder.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }

            if (holder.unitId == null)
            {
                continue;
            }

            var shieldMod = FieldAuraService.FindFieldModule(holder, _modules, "shield_fusion_field");
            var armorMod = FieldAuraService.FindFieldModule(holder, _modules, "armor_link_field");

            if (shieldMod != null && holder.fieldAuraShieldDominant)
            {
                var key = holder.unitId + "|shield";
                expected.Add(key);
                UpdateAuraSphere(holder, shieldMod, key, FieldAuraVfxCatalog.ShieldMaterial, focusWorld);
            }

            if (armorMod != null && holder.fieldAuraArmorDominant)
            {
                var key = holder.unitId + "|armor";
                expected.Add(key);
                UpdateAuraSphere(holder, armorMod, key, FieldAuraVfxCatalog.ArmorMaterial, focusWorld);
            }
        }

        var stale = new List<string>();
        foreach (var key in _active.Keys)
        {
            if (!expected.Contains(key))
            {
                stale.Add(key);
            }
        }

        foreach (var key in stale)
        {
            if (_active.TryGetValue(key, out var go) && go != null)
            {
                Object.Destroy(go);
            }

            _active.Remove(key);
        }

        if (expected.Count != _lastDiagSphereCount)
        {
            _lastDiagSphereCount = expected.Count;
            UnityEngine.Debug.Log(
                $"TopDog field-aura-vfx: {expected.Count} sphere(s) active @ t={bf.timeSec:F1}");
            // #region agent log
            CombatFxAgentLog.Write(
                "A",
                "FieldAuraVfxPresenter.Refresh",
                "spheres-changed",
                "{\"count\":" + expected.Count
                + ",\"t\":" + bf.timeSec.ToString("F1")
                + ",\"focusX\":" + focusWorld.x.ToString("F0")
                + ",\"focusY\":" + focusWorld.y.ToString("F0")
                + ",\"focusZ\":" + focusWorld.z.ToString("F0") + "}");
            // #endregion
        }
    }

    private void UpdateAuraSphere(
        BattlefieldUnit holder,
        ModuleDef mod,
        string key,
        Material material,
        Vector3 focusWorld)
    {
        var created = false;
        if (!_active.TryGetValue(key, out var go) || go == null)
        {
            created = true;
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "field-aura-" + key;
            go.transform.SetParent(_worldRoot, false);
            go.layer = CombatFxCameraHost.CombatFxLayer;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            _active[key] = go;
        }

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
        }

        var hull = holder.hullId != null ? _ships.FindHull(holder.hullId) : null;
        var radiusM = FieldAuraService.ResolveFieldRadiusM(holder, mod, hull);
        var diameter = Mathf.Max(1f, radiusM * 2f);
        // 直接用持有舰世界坐标做球心（不依赖 focus 差值；LateUpdate 仍会再 Snap）
        var center = new Vector3(holder.x, holder.y, holder.z);
        var anchor = go.GetComponent<FieldAuraWorldAnchor>()
                     ?? go.AddComponent<FieldAuraWorldAnchor>();
        anchor.WorldCenter = center;
        go.transform.position = center;
        go.transform.localScale = new Vector3(diameter, diameter, diameter);
        go.SetActive(true);

        if (renderer != null)
        {
            renderer.bounds = new Bounds(center, Vector3.one * diameter * 1.1f);
        }

        // #region agent log
        if (created || Time.frameCount % 60 == 0)
        {
            var wp = go.transform.position;
            var dx = wp.x - holder.x;
            var dy = wp.y - holder.y;
            var dz = wp.z - holder.z;
            var err = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
            CombatFxAgentLog.Write(
                "G",
                "FieldAuraVfxPresenter.UpdateAuraSphere",
                created ? "sphere-created" : "sphere-pos",
                "{\"key\":\"" + key
                + "\",\"radiusM\":" + radiusM.ToString("F0")
                + ",\"errM\":" + err.ToString("F0")
                + ",\"hx\":" + holder.x.ToString("F0")
                + ",\"hz\":" + holder.z.ToString("F0")
                + ",\"wx\":" + wp.x.ToString("F0")
                + ",\"wz\":" + wp.z.ToString("F0")
                + ",\"fx\":" + focusWorld.x.ToString("F0")
                + ",\"fz\":" + focusWorld.z.ToString("F0")
                + ",\"rootX\":" + _worldRoot.position.x.ToString("F0")
                + ",\"rootZ\":" + _worldRoot.position.z.ToString("F0")
                + ",\"phase\":\"Update\"}");
        }
        // #endregion
    }

    /// <summary>FX 相机 LateUpdate 挪动 worldRoot 后，按持有舰坐标重贴球心。</summary>
    public static void SnapAnchorsUnder(Transform? worldRoot)
    {
        if (worldRoot == null)
        {
            return;
        }

        for (var i = 0; i < worldRoot.childCount; i++)
        {
            var child = worldRoot.GetChild(i);
            var anchor = child.GetComponent<FieldAuraWorldAnchor>();
            if (anchor == null)
            {
                continue;
            }

            child.position = anchor.WorldCenter;
        }
    }

    private void HideAll()
    {
        foreach (var go in _active.Values)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }
}
