using System.Collections.Generic;
using TopDog.Client.Tactical;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/INTERDICTION_FIELD.md §4
 * 本文件: InterdictionVfxPresenter.cs — 拦截泡球壳（diam=2×radiusM）
 * 【硬边界】无挡火/弹道改写；仅同步 bf.interdictionSources
 * ══
 */

namespace TopDog.Client;

public sealed class InterdictionVfxPresenter
{
    private readonly Transform _worldRoot;
    private readonly Dictionary<string, GameObject> _active = new();

    public InterdictionVfxPresenter(Transform worldRoot)
    {
        _worldRoot = worldRoot;
    }

    public void Refresh(GameState? state, BattlefieldState? bf, Vector3 focusWorld)
    {
        _ = focusWorld;
        if (!ClientGameSettings.CombatFxEnabled || state == null || bf == null || _worldRoot == null)
        {
            HideAll();
            return;
        }

        var expected = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var source in bf.interdictionSources)
        {
            if (source.expiresAtSec <= bf.timeSec || string.IsNullOrEmpty(source.sourceId))
            {
                continue;
            }

            expected.Add(source.sourceId);
            UpdateSphere(source);
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
    }

    private void UpdateSphere(InterdictionFieldSource source)
    {
        var key = source.sourceId;
        var mat = source.mobile
            ? InterdictionVfxCatalog.MobileMaterial
            : InterdictionVfxCatalog.FixedMaterial;
        if (!_active.TryGetValue(key, out var go) || go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "interdiction-" + key;
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
            if (renderer.sharedMaterial != mat)
            {
                renderer.sharedMaterial = mat;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        var diameter = Mathf.Max(1f, source.radiusM * 2f);
        go.transform.localScale = Vector3.one * diameter;
        var center = new Vector3(source.x, source.y, source.z);
        var anchor = go.GetComponent<FieldAuraWorldAnchor>();
        if (anchor == null)
        {
            anchor = go.AddComponent<FieldAuraWorldAnchor>();
        }

        anchor.WorldCenter = center;
        go.transform.position = center;
    }

    private void HideAll()
    {
        foreach (var go in _active.Values)
        {
            if (go != null)
            {
                Object.Destroy(go);
            }
        }

        _active.Clear();
    }
}
