using TopDog.App;
using TopDog.Client;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_FX.md · docs/FIELD_AURA_MODULES.md §6
 * 本文件: CombatFxCameraHost.cs — 特效相机（只读拉 orbit；独立透明 RT；舰标下）
 * 【机制要点】
 * · Layer CombatFxLayer；Camera.Render → combat-fx-overlay Image
 * · 不写天空 RT、不改 UITK 舰标/输入；pickingMode Ignore
 * · ClientGameSettings.CombatFxEnabled 控制启停
 * 【关联】CombatFxTracerPresenter · FieldAuraVfxPresenter · CombatRealtimeController
 * ══
 */

namespace TopDog.Client.Tactical;

public sealed class CombatFxCameraHost : MonoBehaviour
{
    /// <summary>特效专用层（与历史 FieldAura Layer 29 共用）。</summary>
    public const int CombatFxLayer = 29;

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
    private bool _active;
    private int _rtWidth;
    private int _rtHeight;
    private bool _loggedVisibleRt;
    private bool _loggedEmptyRt;
    private bool _loggedManualDrawOnce;
    private Vector3 _fxCamWorldPos;
    private UnityEngine.Rendering.CommandBuffer? _fxCmd;

    public Transform WorldRoot => _worldRoot != null ? _worldRoot : transform;

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

        CurrentFocusWorld = ResolveFocusWorld();
        if (_worldRoot != null)
        {
            _worldRoot.position = CurrentFocusWorld;
        }
    }

    public void SetActive(bool active)
    {
        var next = active && ClientGameSettings.CombatFxEnabled;
        if (next == _active)
        {
            return;
        }

        var prev = _active;
        _active = next;
        if (_rtImage != null && !_active)
        {
            _rtImage.style.display = DisplayStyle.None;
        }

        if (!_active && _camera != null)
        {
            _camera.targetTexture = null;
        }

        // #region agent log
        CombatFxAgentLog.Write(
            "C",
            "CombatFxCameraHost.SetActive",
            "set-active-changed",
            "{\"prev\":" + (prev ? "true" : "false")
            + ",\"want\":" + (active ? "true" : "false")
            + ",\"enabledPref\":" + (ClientGameSettings.CombatFxEnabled ? "true" : "false")
            + ",\"_active\":" + (_active ? "true" : "false")
            + ",\"hasCam\":" + (_camera != null ? "true" : "false")
            + ",\"hasImg\":" + (_rtImage != null ? "true" : "false") + "}");
        // #endregion
    }

    private void OnEnable() => EnsureCameraRig();

    private void OnDestroy()
    {
        if (_viewportHost != null)
        {
            _viewportHost.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        ReleaseRenderTexture();
        if (_fxCmd != null)
        {
            _fxCmd.Release();
            _fxCmd = null;
        }
    }

    private void LateUpdate()
    {
        if (!_active || _orbitSource == null || _camera == null || _focusRig == null)
        {
            return;
        }

        if (!ClientGameSettings.CombatFxEnabled)
        {
            if (_rtImage != null)
            {
                _rtImage.style.display = DisplayStyle.None;
            }

            return;
        }

        // 无球壳时禁止每帧 Clear/Draw 全屏 RT（主因：指令能下但仿真无反应）
        if (!HasActiveFxChildren())
        {
            if (_rtImage != null)
            {
                _rtImage.style.display = DisplayStyle.None;
            }

            return;
        }

        var stamp = RealtimeInteractionFramePolicy.BeginStamp();
        EnsureRenderTexture();
        if (_renderTexture == null)
        {
            return;
        }

        SyncCameraPose();
        FieldAuraVfxPresenter.SnapAnchorsUnder(_worldRoot);
        // 手动画 Mesh，无需每帧改 Renderer.bounds
        RenderFxMeshesManual();

        if (_rtImage != null)
        {
            if (_rtImage.image != _renderTexture)
            {
                _rtImage.image = _renderTexture;
            }

            _rtImage.style.display = DisplayStyle.Flex;
        }

        // #region agent log
        var lateMs = RealtimeInteractionFramePolicy.ElapsedMs(stamp);
        if (lateMs >= 8f || Time.frameCount % 60 == 0)
        {
            CombatFxAgentLog.Write(
                "F",
                "CombatFxCameraHost.LateUpdate",
                "late-cost",
                "{\"ms\":" + lateMs.ToString("F2")
                + ",\"children\":" + (_worldRoot != null ? _worldRoot.childCount : 0)
                + ",\"rtW\":" + _rtWidth
                + ",\"rtH\":" + _rtHeight + "}");
        }

        if (_orbitSource != null && Time.frameCount % 120 == 0)
        {
            var child = _worldRoot!.GetChild(0);
            var anchor = child.GetComponent<FieldAuraWorldAnchor>();
            var wp = child.position;
            var err = anchor != null ? Vector3.Distance(wp, anchor.WorldCenter) : -1f;
            var slotW = _rtSlot != null ? _rtSlot.worldBound.width : _rtWidth;
            var slotH = _rtSlot != null ? _rtSlot.worldBound.height : _rtHeight;
            var dx = wp.x - CurrentFocusWorld.x;
            var dy = wp.y - CurrentFocusWorld.y;
            var dz = wp.z - CurrentFocusWorld.z;
            var markerProj = _orbitSource.ProjectWorldOffset(dx, dy, dz, slotW, slotH);
            var sp = _camera.WorldToScreenPoint(wp);
            var fxCx = sp.x;
            var fxCy = _rtHeight > 0 ? _rtHeight - sp.y : sp.y;
            CombatFxAgentLog.Write(
                "H2",
                "CombatFxCameraHost.LateUpdate",
                "map-compare",
                "{\"errM\":" + err.ToString("F1")
                + ",\"markerCx\":" + markerProj.CenterX.ToString("F1")
                + ",\"markerCy\":" + markerProj.CenterY.ToString("F1")
                + ",\"fxCx\":" + fxCx.ToString("F1")
                + ",\"fxCy\":" + fxCy.ToString("F1")
                + ",\"dCx\":" + (fxCx - markerProj.CenterX).ToString("F1")
                + ",\"dCy\":" + (fxCy - markerProj.CenterY).ToString("F1")
                + ",\"lateMs\":" + lateMs.ToString("F2")
                + ",\"yFlipFix\":\"gpuProjFalse\"}");
        }

        if (!_loggedVisibleRt)
        {
            _loggedVisibleRt = true;
            CombatFxAgentLog.Write(
                "D",
                "CombatFxCameraHost.LateUpdate",
                "rt-ok",
                "{\"w\":" + _rtWidth + ",\"h\":" + _rtHeight
                + ",\"mode\":\"manual-gated\"}");
        }
        // #endregion
    }

    /// <summary>
    /// 绕过 URP Camera.Render 透明通道：CommandBuffer 直写 RT。
    /// </summary>
    private void RenderFxMeshesManual()
    {
        if (_camera == null || _renderTexture == null || _worldRoot == null)
        {
            return;
        }

        _fxCmd ??= new UnityEngine.Rendering.CommandBuffer { name = "TopDog.CombatFxManual" };
        _fxCmd.Clear();
        _fxCmd.SetRenderTarget(_renderTexture);
        _fxCmd.ClearRenderTarget(true, true, Color.clear);
        var view = _camera.worldToCameraMatrix;
        // UITK Image 按纹理原样拉伸；renderIntoTexture=true 会再翻一次 Y，
        // 导致屏上「舰往下、球往上」。与舰标同向必须用 false。
        var proj = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
        _fxCmd.SetViewProjectionMatrices(view, proj);
        // 手动画不会自动刷新内置相机位置；缘光/斑驳视向量必须跟战术轨道走，否则会像反着转
        _fxCmd.SetGlobalVector("_WorldSpaceCameraPos", _fxCamWorldPos);
        _fxCmd.SetGlobalVector("_FxCamWorldPos", _fxCamWorldPos);

        var drawn = 0;
        for (var i = 0; i < _worldRoot.childCount; i++)
        {
            var child = _worldRoot.GetChild(i);
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            var filter = child.GetComponent<MeshFilter>();
            var renderer = child.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null || !renderer.enabled)
            {
                continue;
            }

            var mesh = filter.sharedMesh;
            var mat = renderer.sharedMaterial;
            if (mesh == null || mat == null)
            {
                continue;
            }

            var matrix = child.localToWorldMatrix;
            var passes = Mathf.Max(1, mat.passCount);
            for (var p = 0; p < passes; p++)
            {
                _fxCmd.DrawMesh(mesh, matrix, mat, 0, p);
                drawn++;
            }
        }

        Graphics.ExecuteCommandBuffer(_fxCmd);

        // #region agent log
        if (!_loggedManualDrawOnce && drawn > 0)
        {
            _loggedManualDrawOnce = true;
            string sh = "none";
            for (var i = 0; i < _worldRoot.childCount; i++)
            {
                var r = _worldRoot.GetChild(i).GetComponent<MeshRenderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    sh = r.sharedMaterial.shader.name.Replace("\\", "/").Replace("\"", "'");
                    break;
                }
            }

            CombatFxAgentLog.Write(
                "A",
                "CombatFxCameraHost.RenderFxMeshesManual",
                "manual-draw-once",
                "{\"drawnPasses\":" + drawn
                + ",\"children\":" + _worldRoot.childCount
                + ",\"shader\":\"" + sh
                + "\",\"camPosZ\":" + _camera.transform.localPosition.z.ToString("F0")
                + ",\"fov\":" + _camera.fieldOfView.ToString("F1") + "}");
        }
        // #endregion
    }

    private bool HasActiveFxChildren()
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

        CurrentFocusWorld = ResolveFocusWorld();
        _focusRig.position = CurrentFocusWorld;
        if (_worldRoot != null)
        {
            _worldRoot.position = CurrentFocusWorld;
        }

        // 姿态不用欧拉链：直接写入与 TacticalViewportCamera.WorldOffsetToViewSpace 同构的矩阵。
        // FOV/视距/aspect 必须与舰标同一套——画实体体积并不要求另一套参数，以前差参是实现分叉不是硬需求。
        if (_yRotationRoot != null)
        {
            _yRotationRoot.localRotation = Quaternion.identity;
        }

        if (_xRotationRoot != null)
        {
            _xRotationRoot.localRotation = Quaternion.identity;
        }

        _camera.transform.localPosition = Vector3.zero;
        _camera.transform.localRotation = Quaternion.identity;

        var yaw = _orbitSource.OrbitYawRad;
        var pitch = _orbitSource.OrbitPitchRad;
        var dist = _orbitSource.ViewDistance;
        var fov = _orbitSource.VerticalFovDeg;
        var aspect = _rtWidth > 0 && _rtHeight > 0
            ? _rtWidth / (float)_rtHeight
            : Mathf.Max(_camera.aspect, 0.01f);

        _camera.fieldOfView = fov;
        _camera.aspect = aspect;
        _camera.nearClipPlane = 10f;
        _camera.farClipPlane = 800_000f;
        _fxCamWorldPos = ComputeTacticalCameraWorld(CurrentFocusWorld, yaw, pitch, dist);
        _camera.worldToCameraMatrix = BuildTacticalWorldToCamera(
            CurrentFocusWorld, yaw, pitch, dist);
        _camera.projectionMatrix = Matrix4x4.Perspective(
            fov, aspect, _camera.nearClipPlane, _camera.farClipPlane);

        // #region agent log
        if (Time.frameCount % 60 == 0)
        {
            CombatFxAgentLog.Write(
                "H1",
                "CombatFxCameraHost.SyncCameraPose",
                "tactical-matrix",
                "{\"fov\":" + fov.ToString("F2")
                + ",\"dist\":" + dist.ToString("F0")
                + ",\"aspect\":" + aspect.ToString("F3")
                + ",\"yaw\":" + yaw.ToString("F3")
                + ",\"pitch\":" + pitch.ToString("F3")
                + ",\"camX\":" + _fxCamWorldPos.x.ToString("F0")
                + ",\"camY\":" + _fxCamWorldPos.y.ToString("F0")
                + ",\"camZ\":" + _fxCamWorldPos.z.ToString("F0")
                + ",\"rtW\":" + _rtWidth
                + ",\"rtH\":" + _rtHeight + "}");
        }
        // #endregion
    }

    /// <summary>战术视点世界坐标：focus − viewDistance × view-forward（与 WorldOffsetToViewSpace 的 vz 轴同构）。</summary>
    private static Vector3 ComputeTacticalCameraWorld(
        Vector3 focus, float yawRad, float pitchRad, float viewDistance)
    {
        var cosY = Mathf.Cos(yawRad);
        var sinY = Mathf.Sin(yawRad);
        var cosP = Mathf.Cos(pitchRad);
        var sinP = Mathf.Sin(pitchRad);
        // view-forward = R row2
        var fx = sinY * cosP;
        var fy = sinP;
        var fz = cosY * cosP;
        return new Vector3(
            focus.x - viewDistance * fx,
            focus.y - viewDistance * fy,
            focus.z - viewDistance * fz);
    }

    /// <summary>
    /// 与 <see cref="TacticalViewportCamera.WorldOffsetToViewSpace"/> 同构；
    /// Unity 相机看向 -Z，故 camZ = -(vz + viewDistance)。
    /// </summary>
    private static Matrix4x4 BuildTacticalWorldToCamera(
        Vector3 focus, float yawRad, float pitchRad, float viewDistance)
    {
        var cosY = Mathf.Cos(yawRad);
        var sinY = Mathf.Sin(yawRad);
        var cosP = Mathf.Cos(pitchRad);
        var sinP = Mathf.Sin(pitchRad);

        var r00 = cosY;
        var r01 = 0f;
        var r02 = -sinY;
        var r10 = -sinY * sinP;
        var r11 = cosP;
        var r12 = -cosY * sinP;
        var r20 = sinY * cosP;
        var r21 = sinP;
        var r22 = cosY * cosP;

        var m = Matrix4x4.identity;
        m.m00 = r00;
        m.m01 = r01;
        m.m02 = r02;
        m.m03 = -(r00 * focus.x + r01 * focus.y + r02 * focus.z);

        m.m10 = r10;
        m.m11 = r11;
        m.m12 = r12;
        m.m13 = -(r10 * focus.x + r11 * focus.y + r12 * focus.z);

        m.m20 = -r20;
        m.m21 = -r21;
        m.m22 = -r22;
        m.m23 = (r20 * focus.x + r21 * focus.y + r22 * focus.z) - viewDistance;

        m.m30 = 0f;
        m.m31 = 0f;
        m.m32 = 0f;
        m.m33 = 1f;
        return m;
    }

    private Vector3 ResolveFocusWorld()
    {
        var core = GameAppHost.Instance?.Core;
        var state = core?.State;
        var bf = _orbitSource?.ActiveBattlefieldProvider?.Invoke();
        if (state != null && bf != null)
        {
            // 与舰标 / 飘字 / 弹道同一套注视点（勿自建 first-friendly 回退）
            var focus = TopDog.Sim.Vision.VisionAnchorService.ResolveDefaultFocus(state, bf);
            if (focus != null)
            {
                return new Vector3(focus.x, focus.y, focus.z);
            }
        }

        return Vector3.zero;
    }

    private void EnsureCameraRig()
    {
        if (_camera == null || _focusRig == null)
        {
            var existing = transform.Find("CombatFxCameraRig");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var rig = new GameObject("CombatFxCameraRig");
            rig.transform.SetParent(transform, false);
            _focusRig = rig.transform;

            var yGo = new GameObject("YRotationRoot");
            yGo.transform.SetParent(_focusRig, false);
            yGo.layer = CombatFxLayer;
            _yRotationRoot = yGo.transform;

            var xGo = new GameObject("XRotationRoot");
            xGo.transform.SetParent(_yRotationRoot, false);
            xGo.layer = CombatFxLayer;
            _xRotationRoot = xGo.transform;

            var camGo = new GameObject("CombatFxCamera");
            camGo.transform.SetParent(_xRotationRoot, false);
            camGo.layer = CombatFxLayer;
            _camera = camGo.AddComponent<Camera>();
            _camera.enabled = false;
            if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                camGo.AddComponent<UniversalAdditionalCameraData>();
            }
        }

        if (_camera != null)
        {
            ConfigureCamera(_camera);
        }
    }

    private static void ConfigureCamera(Camera camera)
    {
        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 1 << CombatFxLayer;
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

        _rtImage = new Image { name = "combat-fx-rt", pickingMode = PickingMode.Ignore };
        _rtImage.AddToClassList("rtcombat-combat-fx-rt");
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
        if (_camera == null)
        {
            return;
        }

        // RT 必须与叠层槽（combat-fx-overlay）同像素尺寸/宽高比，
        // 勿用整视口 host（含右侧栏区域）——否则相对舰标投影会整体错位。
        var sizeSource = _rtSlot != null && _rtSlot.worldBound.width > 8f
            ? _rtSlot
            : _viewportHost;
        if (sizeSource == null)
        {
            return;
        }

        var bounds = sizeSource.worldBound;
        // 半分辨率：全分辨率球壳 RT 曾造成 LateUpdate 数百 ms 尖峰、仿真跳帧（指令像冻住）
        var width = Mathf.Clamp(Mathf.RoundToInt(bounds.width * 0.5f), 128, 1024);
        var height = Mathf.Clamp(Mathf.RoundToInt(bounds.height * 0.5f), 128, 1024);
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
            name = "CombatFxRT",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _camera.aspect = _rtWidth / (float)_rtHeight;
        // #region agent log
        var hostW = _viewportHost != null ? _viewportHost.worldBound.width : -1f;
        var slotW = _rtSlot != null ? _rtSlot.worldBound.width : -1f;
        CombatFxAgentLog.Write(
            "H",
            "CombatFxCameraHost.EnsureRenderTexture",
            "rt-size",
            "{\"rtW\":" + _rtWidth
            + ",\"rtH\":" + _rtHeight
            + ",\"slotW\":" + slotW.ToString("F0")
            + ",\"hostW\":" + hostW.ToString("F0")
            + ",\"aspect\":" + (_rtWidth / (float)_rtHeight).ToString("F3") + "}");
        // #endregion
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

    private void ExpandRendererBoundsForCull()
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
