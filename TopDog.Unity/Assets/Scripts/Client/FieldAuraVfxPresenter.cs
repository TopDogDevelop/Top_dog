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
 * 本文件: FieldAuraVfxPresenter.cs — 场域球体实例（sim→Layer29；⏳ 特效可见性待做·顽固）
 * 【关联】FieldAuraVfxCameraHost · FieldAuraVfxCatalog
 * ══
 */

namespace TopDog.Client;

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
        if (state == null || bf == null || _worldRoot == null)
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
        }
    }

    private void UpdateAuraSphere(
        BattlefieldUnit holder,
        ModuleDef mod,
        string key,
        Material material,
        Vector3 focusWorld)
    {
        if (!_active.TryGetValue(key, out var go) || go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "field-aura-" + key;
            go.transform.SetParent(_worldRoot, false);
            go.layer = FieldAuraVfxCameraHost.FieldAuraLayer;
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
        go.transform.localPosition = new Vector3(holder.x, holder.y, holder.z) - focusWorld;
        go.transform.localScale = new Vector3(diameter, diameter, diameter);
        go.SetActive(true);

        if (renderer != null)
        {
            renderer.bounds = new Bounds(go.transform.position, Vector3.one * diameter * 1.1f);
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
