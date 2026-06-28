using TopDog.Client;
using TopDog.Sim.Realtime;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §5.1 宇宙背景 · docs/CLIENT_GAME_SETTINGS.md §2.2
 * 本文件: CombatSpaceBackgroundCameraHost.cs — SG Cubemap 天空盒 → RT → UITK
 * 【机制要点】（对齐第二银河 SolarSystemCelestialLayerController + CameraTransformController）
 * · SG 六面 Cubemap → Skybox/Cubemap 材质；相机 Y/X 分层 orbit；背景随相机旋转
 * · UITK 层：专用透视 Camera.Render → RenderTexture → art-viewport-bg
 * · FOV 读 ClientGameSettings；RT 长边受 CombatBackgroundMaxResolution 限制
 * 【关联】CombatSpaceBackgroundPresenter · TacticalViewportCamera · CombatBackgroundCatalog
 * ══
 */

// liketoc0de345
namespace TopDog.Client.Tactical;

// liketocoode3a5
/// <summary>
/// SG-style cubemap skybox: Y/X camera rig renders to RT for UITK background slot.
/// </summary>
public sealed class CombatSpaceBackgroundCameraHost : MonoBehaviour
{
    public const float BaseFieldOfView = BattlefieldSceneProxyService.TacticalEdgeBaseFovDeg;

    private const int BackgroundLayer = 30;
    private const float SkySphereRadius = 800f;
    private static readonly int TexId = Shader.PropertyToID("_Tex");

    private enum SkyRenderMode
    {
        None,
        SkyboxClear,
        InteriorSphere,
    }

    private Camera? _camera;
    private Transform? _yRotationRoot;
    private Transform? _xRotationRoot;
    private Transform? _skySphere;
    private MeshRenderer? _skyRenderer;
    private Skybox? _skyboxComponent;
    private RenderTexture? _renderTexture;
    private Material? _skyMaterial;
    private Cubemap? _activeCubemap;
    private Texture2D? _equirectFallback;
    private Image? _rtImage;
    private Image? _equirectImage;
    private VisualElement? _viewportHost;
    private VisualElement? _artSlot;
    private TacticalViewportCamera? _orbitSource;
    private SkyRenderMode _skyRenderMode;
    private bool _active;
    private bool _cameraReady;
    private bool _rtHasRenderedFrame;
    private bool _rtContentValidated;
    private bool _useEquirectUi;
    private string? _appliedSetId;
    private int _rtWidth;
    private int _rtHeight;

    public void Bind(VisualElement viewportHost, VisualElement artSlot, TacticalViewportCamera orbitSource)
    {
        _viewportHost = viewportHost;
        _artSlot = artSlot;
        _orbitSource = orbitSource;
        EnsureCameraRig();
        EnsureRtImage();
        EnsureEquirectImage();
        _viewportHost.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        _artSlot.AddToClassList("rtcombat-space-bg");
        _artSlot.pickingMode = PickingMode.Ignore;
        _artSlot.style.overflow = Overflow.Hidden;
        _artSlot.SendToBack();
    // liketocoode34e
    }

    public void SetActive(bool active)
    {
        _active = active;
        UpdateCameraEnabled();
        if (!active)
        {
            if (_rtImage != null)
            {
                _rtImage.style.display = DisplayStyle.None;
            }

            if (_equirectImage != null)
            {
                _equirectImage.style.display = DisplayStyle.None;
            }

            return;
        }

        if (!string.IsNullOrEmpty(_appliedSetId))
        {
            _rtHasRenderedFrame = false;
            _rtContentValidated = false;
            if (_useEquirectUi)
            {
                var cubemap = CombatBackgroundCatalog.LoadCubemap(_appliedSetId, mainPoolOnly: true);
                if (cubemap != null && ApplySkyMaterial(cubemap))
                {
                    _cameraReady = true;
                    _useEquirectUi = false;
                }
            }

            ApplyDisplayMode();
        }
    }

    public void Refresh(string? setId)
    {
        if (string.IsNullOrEmpty(setId))
        {
            _appliedSetId = null;
            _equirectFallback = null;
            _useEquirectUi = false;
            _cameraReady = false;
            _rtHasRenderedFrame = false;
            _rtContentValidated = false;
            ClearArtSlot();
            UpdateCameraEnabled();
            return;
        // liketoco0de345
        }

        EnsureCameraRig();
        EnsureRtImage();
        EnsureEquirectImage();
        if (!setId.Equals(_appliedSetId, System.StringComparison.Ordinal))
        {
            _rtHasRenderedFrame = false;
            _rtContentValidated = false;
            var cubemap = CombatBackgroundCatalog.LoadCubemap(setId, mainPoolOnly: true);
            _equirectFallback = CombatBackgroundCatalog.LoadPanorama(setId, mainPoolOnly: true);
            if (cubemap == null && _equirectFallback == null)
            {
                Debug.LogWarning("TopDog: combat sky cubemap missing for " + setId);
                ClearArtSlot();
                return;
            }

            _cameraReady = cubemap != null && ApplySkyMaterial(cubemap);
            _useEquirectUi = !_cameraReady && _equirectFallback != null;
            if (!_cameraReady && _equirectFallback == null)
            {
                Debug.LogWarning("TopDog: combat skybox material unavailable for " + setId);
                ClearArtSlot();
                return;
            }

            _appliedSetId = setId;
            Debug.Log("TopDog: combat sky loaded " + setId
                + (_cameraReady ? " (" + _skyRenderMode + ")" : " (equirect UI)"));
        }
        else
        {
            ReconcileStaleSkyState(setId);
        }

        if (_useEquirectUi)
        {
            SyncEquirectUi();
        }

        EnsureRenderTexture();
        ApplyDisplayMode();
        UpdateCameraEnabled();
    // liketocoode3e5
    }

    public float CurrentVerticalFovDeg => ClientGameSettings.CombatVerticalFovDeg;

    private void OnEnable()
    {
        ClientGameSettings.CombatBackgroundResolutionChanged += OnBackgroundResolutionChanged;
        ReconcileCameraRigAfterReload();
    }

    private void OnDisable()
    {
        ClientGameSettings.CombatBackgroundResolutionChanged -= OnBackgroundResolutionChanged;
        UpdateCameraEnabled();
    }

    private void OnDestroy()
    {
        if (_viewportHost != null)
        {
            _viewportHost.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        if (_skyMaterial != null)
        {
            Destroy(_skyMaterial);
        }

        ReleaseRenderTexture();
    }

    private void LateUpdate()
    {
        if (!_active || _orbitSource == null || string.IsNullOrEmpty(_appliedSetId))
        {
            return;
        // li3etocoode345
        }

        if (_useEquirectUi || !_cameraReady || _camera == null)
        {
            SyncEquirectUi();
            ApplyDisplayMode();
            return;
        }

        EnsureRenderTexture();
        if (_renderTexture == null)
        {
            return;
        }

        SyncOrbitAndZoom();
        if (_skyRenderMode == SkyRenderMode.InteriorSphere && _skySphere != null)
        {
            _skySphere.position = _camera.transform.position;
            _skySphere.rotation = Quaternion.identity;
        }

        _camera.Render();
        if (!_rtContentValidated && _renderTexture != null)
        {
            if (RenderTextureHasSkyContent(_renderTexture))
            {
                _rtContentValidated = true;
                _rtHasRenderedFrame = true;
            }
            else if (_activeCubemap != null)
            {
                if (_skyRenderMode == SkyRenderMode.SkyboxClear
                    && ApplyInteriorSphereMaterial(_activeCubemap))
                {
                    _camera.Render();
                    _rtContentValidated = RenderTextureHasSkyContent(_renderTexture);
                    _rtHasRenderedFrame = _rtContentValidated;
                }
                else if (_equirectFallback != null)
                {
                    _useEquirectUi = true;
                    _cameraReady = false;
                    SyncEquirectUi();
                    ApplyDisplayMode();
                    return;
                }
            }
        }
        else
        {
            _rtHasRenderedFrame = true;
        }

        if (_rtHasRenderedFrame && _rtImage != null && _renderTexture != null)
        {
            _rtImage.image = _renderTexture;
            _rtImage.MarkDirtyRepaint();
        }

        ApplyDisplayMode();
    }

    private bool ShouldShowRt() =>
        _cameraReady && !_useEquirectUi && _rtHasRenderedFrame && _renderTexture != null;

    private void OnViewportGeometryChanged(GeometryChangedEvent _)
    {
        EnsureRenderTexture();
        if (_useEquirectUi || !ShouldShowRt())
        {
            SyncEquirectUi();
        }

        ApplyDisplayMode();
    }

    private void EnsureRtImage()
    {
        if (_artSlot == null || _rtImage != null)
        {
            return;
        }

        _rtImage = new Image { name = "combat-bg-rt", pickingMode = PickingMode.Ignore };
        _rtImage.AddToClassList("rtcombat-space-bg-image");
        _rtImage.style.position = Position.Absolute;
        _rtImage.style.left = 0;
        _rtImage.style.right = 0;
        _rtImage.style.top = 0;
        _rtImage.style.bottom = 0;
        _rtImage.scaleMode = ScaleMode.StretchToFill;
        _artSlot.Add(_rtImage);
        _rtImage.SendToBack();
    }

    private void EnsureEquirectImage()
    {
        if (_artSlot == null || _equirectImage != null)
        {
            return;
        }

        _equirectImage = new Image { name = "combat-bg-equirect", pickingMode = PickingMode.Ignore };
        _equirectImage.AddToClassList("rtcombat-space-bg-image");
        _equirectImage.style.position = Position.Absolute;
        _equirectImage.style.right = StyleKeyword.Initial;
        _equirectImage.style.bottom = StyleKeyword.Initial;
        _equirectImage.scaleMode = ScaleMode.StretchToFill;
        _artSlot.Add(_equirectImage);
        _equirectImage.SendToBack();
    }

    private bool IsCameraRigValid() =>
        _camera != null && _yRotationRoot != null && _xRotationRoot != null && _skyRenderer != null;

    private void EnsureCameraRig()
    {
        if (IsCameraRigValid())
        {
            EnsureSkyboxComponent();
            return;
        }

        var existing = transform.Find("CombatBackgroundWorld");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        _camera = null;
        _yRotationRoot = null;
        _xRotationRoot = null;
        _skySphere = null;
        _skyRenderer = null;
        _skyboxComponent = null;

        var root = new GameObject("CombatBackgroundWorld");
        root.transform.SetParent(transform, false);
        root.layer = BackgroundLayer;

        var yGo = new GameObject("YRotationRoot");
        yGo.transform.SetParent(root.transform, false);
        yGo.layer = BackgroundLayer;
        _yRotationRoot = yGo.transform;

        var xGo = new GameObject("XRotationRoot");
        xGo.transform.SetParent(_yRotationRoot, false);
        xGo.layer = BackgroundLayer;
        _xRotationRoot = xGo.transform;

        var camGo = new GameObject("CombatBackgroundCamera");
        camGo.transform.SetParent(_xRotationRoot, false);
        camGo.layer = BackgroundLayer;
        _camera = camGo.AddComponent<Camera>();
        _camera.orthographic = false;
        _camera.clearFlags = CameraClearFlags.Skybox;
        _camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
        _camera.cullingMask = 1 << BackgroundLayer;
        _camera.depth = 0;
        _camera.fieldOfView = BaseFieldOfView;
        _camera.nearClipPlane = 0.3f;
        _camera.farClipPlane = SkySphereRadius * 2f;
        _camera.enabled = false;
        _skyboxComponent = _camera.gameObject.AddComponent<Skybox>();

        var sphereGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereGo.name = "CombatSkySphere";
        sphereGo.transform.SetParent(root.transform, false);
        sphereGo.layer = BackgroundLayer;
        Destroy(sphereGo.GetComponent<Collider>());
        _skySphere = sphereGo.transform;
        _skySphere.localScale = Vector3.one * (SkySphereRadius * 2f);
        _skyRenderer = sphereGo.GetComponent<MeshRenderer>();
        _skyRenderer.enabled = false;
    }

    private void ApplyDisplayMode()
    {
        if (_artSlot == null)
        {
            return;
        }

        var showRt = ShouldShowRt();
        if (_equirectImage != null)
        {
            _equirectImage.style.display = showRt ? DisplayStyle.None : DisplayStyle.Flex;
            if (!showRt && _equirectFallback != null)
            {
                _equirectImage.image = _equirectFallback;
            }
        }

        _artSlot.style.backgroundImage = StyleKeyword.None;
        _artSlot.style.backgroundColor = new StyleColor(new Color(0.02f, 0.03f, 0.05f, 1f));

        if (_rtImage != null)
        {
            _rtImage.style.display = showRt ? DisplayStyle.Flex : DisplayStyle.None;
            if (showRt && _renderTexture != null)
            {
                _rtImage.image = _renderTexture;
            }
        }
    }

    private void ReconcileCameraRigAfterReload()
    {
        if (IsCameraRigValid())
        {
            EnsureSkyboxComponent();
            return;
        }

        _camera = null;
        _yRotationRoot = null;
        _xRotationRoot = null;
        _skySphere = null;
        _skyRenderer = null;
        _skyboxComponent = null;
        _cameraReady = false;
        _useEquirectUi = false;
        _rtHasRenderedFrame = false;
        _rtContentValidated = false;
        ReleaseRenderTexture();
    }

    /// <summary>Same setId but material references may be stale after domain reload.</summary>
    private void ReconcileStaleSkyState(string setId)
    {
        _equirectFallback ??= CombatBackgroundCatalog.LoadPanorama(setId, mainPoolOnly: true);

        if (_cameraReady && (_activeCubemap == null || _skyMaterial == null))
        {
            var cubemap = CombatBackgroundCatalog.LoadCubemap(setId, mainPoolOnly: true);
            _cameraReady = cubemap != null && ApplySkyMaterial(cubemap);
            _useEquirectUi = !_cameraReady && _equirectFallback != null;
            _rtHasRenderedFrame = false;
            _rtContentValidated = false;
        }
    }

    private Skybox? EnsureSkyboxComponent()
    {
        if (_camera == null)
        {
            return null;
        }

        if (_skyboxComponent == null)
        {
            _skyboxComponent = _camera.GetComponent<Skybox>();
        }

        if (_skyboxComponent == null)
        {
            _skyboxComponent = _camera.gameObject.AddComponent<Skybox>();
        }

        return _skyboxComponent;
    }

    private bool TryApplySkyboxClearMaterial(Cubemap cubemap)
    {
        var skyboxShader = Shader.Find("Skybox/Cubemap");
        if (skyboxShader == null || _camera == null)
        {
            return false;
        }

        if (_skyMaterial == null)
        {
            _skyMaterial = new Material(skyboxShader);
        }
        else
        {
            _skyMaterial.shader = skyboxShader;
        }

        _skyMaterial.SetTexture(TexId, cubemap);
        _activeCubemap = cubemap;

        var skybox = EnsureSkyboxComponent();
        if (skybox == null)
        {
            return false;
        }

        skybox.material = _skyMaterial;
        _camera.clearFlags = CameraClearFlags.Skybox;
        if (_skyRenderer != null)
        {
            _skyRenderer.enabled = false;
        }

        _skyRenderMode = SkyRenderMode.SkyboxClear;
        return true;
    }

    private bool ApplyInteriorSphereMaterial(Cubemap cubemap)
    {
        var interiorShader = Shader.Find("TopDog/CombatSkyboxInterior");
        if (interiorShader == null || _skyRenderer == null || _camera == null)
        {
            return false;
        }

        if (_skyMaterial == null)
        {
            _skyMaterial = new Material(interiorShader);
        }
        else
        {
            _skyMaterial.shader = interiorShader;
        }

        _skyMaterial.SetTexture(TexId, cubemap);
        _activeCubemap = cubemap;
        _skyRenderer.sharedMaterial = _skyMaterial;
        _skyRenderer.enabled = true;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _skyRenderMode = SkyRenderMode.InteriorSphere;
        return true;
    }

    private bool ApplySkyMaterial(Cubemap cubemap)
    {
        if (TryApplySkyboxClearMaterial(cubemap))
        {
            return true;
        }

        return ApplyInteriorSphereMaterial(cubemap);
    }

    private static bool RenderTextureHasSkyContent(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var probe = new Texture2D(4, 4, TextureFormat.RGB24, false);
        var x = Mathf.Max(0, rt.width / 2 - 2);
        var y = Mathf.Max(0, rt.height / 2 - 2);
        probe.ReadPixels(new Rect(x, y, 4, 4), 0, 0);
        probe.Apply();
        RenderTexture.active = prev;
        var bright = false;
        foreach (var p in probe.GetPixels())
        {
            if (p.r > 0.12f || p.g > 0.12f || p.b > 0.14f)
            {
                bright = true;
                break;
            }
        }

        Destroy(probe);
        return bright;
    }

    private void SyncEquirectUi()
    {
        if (_equirectFallback == null || _orbitSource == null || ShouldShowRt())
        {
            return;
        }

        var bounds = _viewportHost != null ? _viewportHost.worldBound : _artSlot?.worldBound ?? default;
        var viewW = Mathf.Max(bounds.width, 1f);
        var viewH = Mathf.Max(bounds.height, 1f);
        var aspect = viewW / viewH;
        var horizCover = Mathf.Max(200f, aspect * 100f);
        var vertCover = Mathf.Max(100f, (2f / aspect) * 100f);
        var yaw01 = Mathf.Repeat(_orbitSource.OrbitYawRad / (Mathf.PI * 2f), 1f);
        var pitchT = Mathf.InverseLerp(
            TacticalViewportCamera.DefaultOrbitPitchRad - 1.35f,
            TacticalViewportCamera.DefaultOrbitPitchRad + 1.35f,
            _orbitSource.OrbitPitchRad);

        if (_equirectImage != null)
        {
            _equirectImage.image = _equirectFallback;
            _equirectImage.style.width = Length.Percent(horizCover);
            _equirectImage.style.height = Length.Percent(vertCover);
            _equirectImage.style.left = Length.Percent(-yaw01 * (horizCover - 100f));
            _equirectImage.style.top = Length.Percent(Mathf.Lerp(-(vertCover - 100f) * 0.35f, 0f, pitchT));
        }
    }

    private void EnsureRenderTexture()
    {
        if (!_cameraReady || _viewportHost == null || _artSlot == null || _camera == null)
        {
            return;
        // liket0coode345
        }

        var bounds = _viewportHost.worldBound;
        var width = Mathf.RoundToInt(bounds.width);
        var height = Mathf.RoundToInt(bounds.height);
        var maxRes = ClientGameSettings.CombatBackgroundMaxResolution;
        if (width > 0 && height > 0)
        {
            var longest = Mathf.Max(width, height);
            if (longest > maxRes)
            {
                var scale = maxRes / (float)longest;
                width = Mathf.RoundToInt(width * scale);
                height = Mathf.RoundToInt(height * scale);
            }
        }

        width = Mathf.Clamp(width, 128, maxRes);
        height = Mathf.Clamp(height, 128, maxRes);
        if (width < 8 || height < 8)
        {
            UpdateCameraEnabled();
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
            name = "CombatBackgroundRT",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _camera.aspect = _rtWidth / (float)_rtHeight;
        if (_rtImage != null)
        {
            _rtImage.image = _renderTexture;
        }
    }

    private void SyncOrbitAndZoom()
    {
        if (_camera == null || _orbitSource == null)
        {
            return;
        }

        // 战术 pitch=π/2 为俯视默认 → 天空盒水平；与 WorldOffsetToViewSpace 的 X 旋转对齐
        var yawDeg = -_orbitSource.OrbitYawRad * Mathf.Rad2Deg;
        var pitchDeg = (Mathf.PI * 0.5f - _orbitSource.OrbitPitchRad) * Mathf.Rad2Deg;
        if (_yRotationRoot != null)
        {
            _yRotationRoot.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
        }

        if (_xRotationRoot != null)
        {
            _xRotationRoot.localRotation = Quaternion.Euler(pitchDeg, 0f, 0f);
        }

        _camera.fieldOfView = CurrentVerticalFovDeg;
    }

    private void UpdateCameraEnabled()
    {
        if (_camera == null)
        {
            return;
        }

        _camera.enabled = false;
    }

    private void ClearArtSlot()
    {
        if (_artSlot == null)
        {
            return;
        }

        _artSlot.style.backgroundImage = StyleKeyword.None;
        _artSlot.style.backgroundColor = new StyleColor(new Color(0.03f, 0.04f, 0.06f, 1f));
        _rtHasRenderedFrame = false;
        _rtContentValidated = false;
        if (_rtImage != null)
        {
            _rtImage.image = null;
            _rtImage.style.display = DisplayStyle.None;
        }

        if (_equirectImage != null)
        {
            _equirectImage.image = null;
            _equirectImage.style.display = DisplayStyle.None;
        }
    }

    private void OnBackgroundResolutionChanged()
    {
        _rtHasRenderedFrame = false;
        _rtContentValidated = false;
        ReleaseRenderTexture();
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
        _rtHasRenderedFrame = false;
        _rtContentValidated = false;
    // lik3tocoode345
    }
}
// liketocoode3a5
