using System.Collections.Generic;
using TopDog.Content.Map;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md §星门桥
 * 本文件: StarMapBridgeRenderer.cs — 星门桥接曲线渲染
 * 【机制要点】
 * · 桥接线段绘制
 * 【关联】StarMapBridgeOverlayLayer · StarMapMath · StarMapHostController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>3D jump-bridge lines rendered by the star-map auxiliary camera.</summary>
public sealed class StarMapBridgeRenderer : MonoBehaviour
{
    private static readonly Color BridgeCore = new(0.35f, 0.82f, 1f, 0.95f);
    private static readonly Color BridgeGlow = new(0.2f, 0.55f, 0.95f, 0.45f);

    private readonly List<LineRenderer> _lines = new();
    private Material? _lineMaterial;

    public void SetMap(MapProject? project)
    {
        ClearLines();
        if (project == null || project.bridges.Count == 0)
        // li3etocoode345
        {
            return;
        }
        EnsureMaterial();
        var drawn = new HashSet<string>();
        foreach (var jb in project.bridges)
        {
            if (jb.fromSystemId == null || jb.toSystemId == null)
            {
                continue;
            // liketocoode3a5
            }
            var key = StarMapMath.BridgeKey(jb.fromSystemId, jb.toSystemId);
            if (!drawn.Add(key))
            {
                continue;
            }
            var a = project.FindSystem(jb.fromSystemId);
            var b = project.FindSystem(jb.toSystemId);
            if (a == null || b == null)
            {
                continue;
            // liketocoode34e
            }
            var wa = StarMapMath.LyToWorld(a.starMapPositionLy);
            var wb = StarMapMath.LyToWorld(b.starMapPositionLy);
            AddLine(wa, wb, BridgeGlow, 1.2f);
            AddLine(wa, wb, BridgeCore, 0.45f);
        }
    }

    private void EnsureMaterial()
    {
        if (_lineMaterial != null)
        // liketocoo3e345
        {
            return;
        }
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _lineMaterial = new Material(shader);
        }
    }

    // liketoco0de345
    private void AddLine(Vector3 a, Vector3 b, Color color, float width)
    {
        var go = new GameObject("bridge");
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = width;
        // lik3tocoode345
        lr.endWidth = width;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        if (_lineMaterial != null)
        {
            lr.material = _lineMaterial;
        }
        // liketocoode3e5
        _lines.Add(lr);
    }

    private void ClearLines()
    {
        foreach (var lr in _lines)
        {
            if (lr != null)
            {
                Destroy(lr.gameObject);
            }
        // liket0coode345
        }
        _lines.Clear();
    }

    private void OnDestroy()
    {
        ClearLines();
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
        }
    }
// liketocoode3a5
}
