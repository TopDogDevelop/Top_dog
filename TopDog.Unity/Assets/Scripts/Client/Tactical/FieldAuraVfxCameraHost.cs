using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FIELD_AURA_MODULES.md §6 · OPEN_DESIGN D-VFX-01
 * 本文件: FieldAuraVfxCameraHost.cs — 场域 VFX 相机（⏳ 待做 · 🔴 顽固：主工程仍不可见）
 * 【管线】
 * · Layer 29 球体；默认 CompositeOnSkybox（天空 RT + Depth 二 pass）；透明 RT 仅回退
 * · 与 TacticalViewportCamera 同步 orbit / 视距 / 注视点
 * · 验收未达成前勿标「已实现」；分流见专文 §6.3
 * 【关联】FieldAuraVfxPresenter · CombatSpaceBackgroundCameraHost · FieldAuraVfxDemo
 * ══
 */

namespace TopDog.Client.Tactical;

public sealed class FieldAuraVfxCameraHost : MonoBehaviour
{
    public const int FieldAuraLayer = 29;

    public enum FieldAuraRenderMode
    {
        /// <summary>绘制到天空盒 RT 上（推荐，避开透明 RT）。</summary>
        CompositeOnSkybox,
        /// <summary>独立透明 RT → UITK 叠层（旧路径）。</summary>
        SeparateTransparentOverlay,
    }

    private Camera? _camera;
    private Transform? _focusRig;
    private Transform? _yRotationRoot;
    private Transform? _xRotationRoot;
    private Transform? _worldRoot;
    private RenderTexture? _renderTexture;
    private Image? _rtImage;
    private VisualElement? _viewportHost;
    private VisualElement? _rtSlot;
    private TacticalViewportCamera? _orbitSource;
    private FieldAuraRenderMode _renderMode = FieldAuraRenderMode.CompositeOnSkybox;
    private bool _active;
    private bool _emptyRtWarned;
    private bool _visibleRtLogged;
    private int _rtWidth;
    private int _rtHeight;

    public Transform WorldRoot => _worldRoot != null ? _worldRoot : transform;

    /// <summary>与 <see cref="SyncCameraPose"/> 同步的注视点（世界坐标）。</summary>
    public Vector3 CurrentFocusWorld { get; private set; }

    public void Bind(
        VisualElement viewportHost,
        VisualElement rtSlot,
        TacticalViewportCamera orbitSource,
        Transform worldRoot)
    {
        _viewportHost = viewportHost;
        _rtSlot = rtSlot;
        _orbitSource = orbitSource;
        _worldRoot = worldRoot;
        EnsureCameraRig();
        EnsureRtImage();
        _viewportHost.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        _rtSlot.pickingMode = PickingMode.Ignore;
    }

    public void PrepareWorldRootForFrame()
    {
        if (_orbitSource == null)
        {
            return;
        }

        var focus = ResolveFocus();
        CurrentFocusWorld = new Vector3(focus.x, focus.y, focus.z);
        if (_worldRoot != null)
        {
            _worldRoot.position = CurrentFocusWorld;
        }
    }

    public void SetRenderMode(FieldAuraRenderMode mode)
    {
        _renderMode = mode;
        if (_rtImage != null)
        {
            _rtImage.style.display = mode == FieldAuraRenderMode.SeparateTransparentOverlay
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (_rtImage == null)
        {
            return;
        }

        if (!active || _renderMode == FieldAuraRenderMode.CompositeOnSkybox)
        {
            _rtImage.style.display = DisplayStyle.None;
        }
    }

    private void OnEnable()
    {
        EnsureCameraRig();
    }

    private void OnDestroy()
    {
        if (_viewportHost != null)
        {
            _viewportHost.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        ReleaseRenderTexture();
    }

    private void LateUpdate()
    {
        if (!_active || _orbitSource == null || _camera == null || _focusRig == null)
        {
            return;
        }

        if (_renderMode == FieldAuraRenderMode.CompositeOnSkybox)
        {
            return;
        }

        EnsureRenderTexture();
        if (_renderTexture == null)
        {
            return;
        }

        RenderSeparateOverlayPass();
    }

    /// <summary>天空盒 RT 绘制完成后调用：保留颜色、清深度，叠场域球。</summary>
    public void RenderCompositeOntoSkybox(RenderTexture skyRenderTarget)
    {
        if (!_active
            || _renderMode != FieldAuraRenderMode.CompositeOnSkybox
            || _orbitSource == null
            || _camera == null
            || _focusRig == null
            || skyRenderTarget == null
            || !HasActiveSpheres())
        {
            return;
        }

        SyncCameraPose();
        ExpandSphereBoundsForCull();

        var prevTarget = _camera.targetTexture;
        var prevClear = _camera.clearFlags;
        var prevAspect = _camera.aspect;
        _camera.targetTexture = skyRenderTarget;
        _camera.clearFlags = CameraClearFlags.Depth;
        _camera.aspect = skyRenderTarget.width / (float)skyRenderTarget.height;
        _camera.Render();
        _camera.targetTexture = prevTarget;
        _camera.clearFlags = prevClear;
        _camera.aspect = prevAspect;

        if (!_visibleRtLogged && RenderTextureHasAuraContent(skyRenderTarget))
        {
            _visibleRtLogged = true;
            _emptyRtWarned = false;
            Debug.Log("TopDog field-aura-vfx: composite on skybox OK — field aura pixels visible");
        }
        else if (!_emptyRtWarned && !_visibleRtLogged)
        {
            _emptyRtWarned = true;
            Debug.LogWarning(
                "TopDog field-aura-vfx: composite pass drew no visible pixels "
                + "(Layer29 / FieldAuraSphere / camera pose)");
        }
    }

    private void RenderSeparateOverlayPass()
    {
        if (_renderTexture == null || _camera == null)
        {
            return;
        }

        SyncCameraPose();
        ExpandSphereBoundsForCull();
        _camera.Render();
        if (_rtImage != null && _renderTexture != null)
        {
            _rtImage.image = _renderTexture;
            _rtImage.style.display = DisplayStyle.Flex;
            _rtImage.MarkDirtyRepaint();
            _rtSlot?.BringToFront();
            _rtSlot?.MarkDirtyRepaint();
        }

        var hasContent = RenderTextureHasAuraContent(_renderTexture);
        if (hasContent && !_visibleRtLogged)
        {
            _visibleRtLogged = true;
            Debug.Log("TopDog field-aura-vfx: RT probe OK — field aura pixels visible");
        }

        if (!_emptyRtWarned && !hasContent)
        {
            _emptyRtWarned = true;
            Debug.LogWarning(
                "TopDog field-aura-vfx: RT probe found no visible pixels "
                + "(check Layer29 spheres, TopDog/FieldAuraSphere shader, viewport stack order)");
        }
    }

    private bool HasActiveSpheres()
    {
        if (_worldRoot == null)
        {
            return false;
        }

        for (var i = 0; i < _worldRoot.childCount; i++)
        {
            if (_worldRoot.GetChild(i).gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private void SyncCameraPose()
    {
        if (_orbitSource == null || _focusRig == null || _camera == null)
        {
            return;
        }

        var focus = ResolveFocus();
        CurrentFocusWorld = new Vector3(focus.x, focus.y, focus.z);
        _focusRig.position = CurrentFocusWorld;
        if (_worldRoot != null)
        {
            // 子球体仍用 sim 世界坐标；父节点跟注视点 → 相对相机为近原点几何，减轻 km 尺度精度/剔除问题
            _worldRoot.position = CurrentFocusWorld;
        }

        var yawDeg = -_orbitSource.OrbitYawRad * Mathf.Rad2Deg;
        var pitchDeg = (Mathf.PI * 0.5f - _orbitSource.OrbitPitchRad) * Mathf.Rad2Deg;
        if (_yRotationRoot != null)
        {
            _yRotationRoot.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
        }

        if (_xRotationRoot != null)
        {
            _xRotationRoot.localRotation = Quaternion.Euler(-pitchDeg, 0f, 0f);
        }

        _camera.fieldOfView = _orbitSource.VerticalFovDeg;
        _camera.transform.localPosition = new Vector3(0f, 0f, -_orbitSource.ViewDistance);
    }

    private (float x, float y, float z) ResolveFocus()
    {
        var core = GameAppHost.Instance?.Core;
        var state = core?.State;
        var bf = _orbitSource?.ActiveBattlefieldProvider?.Invoke();
        if (state != null && bf != null)
        {
            if (state.tacticalCameraUnitId != null)
            {
                foreach (var u in bf.units)
                {
                    if (state.tacticalCameraUnitId.Equals(u.unitId, System.StringComparison.Ordinal))
                    {
                        return (u.x, u.y, u.z);
                    }
                }
            }

            foreach (var u in bf.units)
            {
                if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && !u.isBuilding)
                {
                    return (u.x, u.y, u.z);
                }
            }
        }

        return (0f, 0f, 0f);
    }

    private void EnsureCameraRig()
    {
        if (_camera == null || _focusRig == null)
        {
            var existing = transform.Find("FieldAuraCameraRig");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var rig = new GameObject("FieldAuraCameraRig");
            rig.transform.SetParent(transform, false);

            _focusRig = rig.transform;

            var yGo = new GameObject("YRotationRoot");
            yGo.transform.SetParent(_focusRig, false);
            yGo.layer = FieldAuraLayer;
            _yRotationRoot = yGo.transform;

            var xGo = new GameObject("XRotationRoot");
            xGo.transform.SetParent(_yRotationRoot, false);
            xGo.layer = FieldAuraLayer;
            _xRotationRoot = xGo.transform;

            var camGo = new GameObject("FieldAuraCamera");
            camGo.transform.SetParent(_xRotationRoot, false);
            camGo.layer = FieldAuraLayer;
            _camera = camGo.AddComponent<Camera>();
            _camera.enabled = false;
            if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                camGo.AddComponent<UniversalAdditionalCameraData>();
            }
        }

        if (_camera != null)
        {
            ConfigureTransparentRtCamera(_camera);
        }
    }

    private static void ConfigureTransparentRtCamera(Camera camera)
    {
        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << FieldAuraLayer;
        camera.depth = 2;
        camera.nearClipPlane = 10f;
        camera.farClipPlane = 800_000f;
        camera.allowHDR = false;
        camera.allowMSAA = false;

        var urpData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
        {
            return;
        }

        urpData.renderShadows = false;
        urpData.renderPostProcessing = false;
        urpData.antialiasing = AntialiasingMode.None;
        urpData.requiresColorOption = CameraOverrideOption.Off;
        urpData.requiresDepthOption = CameraOverrideOption.Off;
    }

    private void EnsureRtImage()
    {
        if (_rtSlot == null || _rtImage != null)
        {
            return;
        }

        _rtImage = new Image { name = "field-aura-rt", pickingMode = PickingMode.Ignore };
        _rtImage.AddToClassList("rtcombat-field-aura-rt");
        _rtImage.style.position = Position.Absolute;
        _rtImage.style.left = 0;
        _rtImage.style.right = 0;
        _rtImage.style.top = 0;
        _rtImage.style.bottom = 0;
        _rtImage.scaleMode = ScaleMode.StretchToFill;
        _rtImage.style.display = DisplayStyle.None;
        _rtImage.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
        _rtImage.style.backgroundColor = new StyleColor(Color.clear);
        _rtSlot.Add(_rtImage);
    }

    private void OnViewportGeometryChanged(GeometryChangedEvent _) => EnsureRenderTexture();

    private void EnsureRenderTexture()
    {
        if (_viewportHost == null || _camera == null)
        {
            return;
        }

        var bounds = _viewportHost.worldBound;
        var width = Mathf.RoundToInt(bounds.width);
        var height = Mathf.RoundToInt(bounds.height);
        width = Mathf.Clamp(width, 128, 2048);
        height = Mathf.Clamp(height, 128, 2048);
        if (width < 8 || height < 8)
        {
            return;
        }

        if (_renderTexture != null && _rtWidth == width && _rtHeight == height)
        {
            return;
        }

        ReleaseRenderTexture();
        _rtWidth = width;
        _rtHeight = height;
        _renderTexture = new RenderTexture(_rtWidth, _rtHeight, 24, RenderTextureFormat.ARGB32)
        {
            name = "FieldAuraVfxRT",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _camera.aspect = _rtWidth / (float)_rtHeight;
        _emptyRtWarned = false;
        _visibleRtLogged = false;
    }

    private void ReleaseRenderTexture()
    {
        if (_camera != null)
        {
            _camera.targetTexture = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        _rtWidth = 0;
        _rtHeight = 0;
    }

    private static bool RenderTextureHasAuraContent(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var probe = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var x = Mathf.Max(0, rt.width / 2 - 2);
        var y = Mathf.Max(0, rt.height / 2 - 2);
        probe.ReadPixels(new Rect(x, y, 4, 4), 0, 0);
        probe.Apply();
        RenderTexture.active = prev;
        var visible = false;
        foreach (var p in probe.GetPixels())
        {
            if (p.a > 0.05f || (p.r + p.g + p.b) > 0.12f)
            {
                visible = true;
                break;
            }
        }

        Destroy(probe);
        return visible;
    }

    private void ExpandSphereBoundsForCull()
    {
        if (_worldRoot == null)
        {
            return;
        }

        for (var i = 0; i < _worldRoot.childCount; i++)
        {
            var child = _worldRoot.GetChild(i);
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            var diameter = Mathf.Max(1f, child.lossyScale.x);
            renderer.bounds = new Bounds(child.position, Vector3.one * diameter * 1.1f);
        }
    }
}
