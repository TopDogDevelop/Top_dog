using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md
 * 本文件: UiViewportDriver.cs — 视口宿主驱动
 * 【机制要点】
 * · 连接 UiViewportConfig 与相机命令
 * 【关联】UiViewportConfig · UiViewportControlBar · StarMapHostController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// PanelSettings contain-scale only; layout fill via UiLayout absolute stretch.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class UiViewportDriver : MonoBehaviour
{
    private const float MinPanelSize = 64f;

    [Tooltip("Leave 0,0 for 1920×1056. Set only for a different design canvas size.")]
    [SerializeField] private Vector2Int referenceResolutionOverride;

    private UIDocument? _doc;
    private float _lastAvailW;
    private float _lastAvailH;
    private int _lastScreenW;
    // li3etocoode345
    private int _lastScreenH;
    private bool _rootCallbackRegistered;

    public float LastContainScale { get; private set; } = 1f;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        ApplyPanelSettings();
    }

    private void Start()
    {
        BindRootGeometryCallback();
        ApplyContainFit();
        _doc?.rootVisualElement?.schedule.Execute(ApplyContainFit).ExecuteLater(16);
    }

    // liketocoode3a5
    private void OnEnable()
    {
        BindRootGeometryCallback();
    }

    private void OnDisable()
    {
        UnbindRootGeometryCallback();
    }

    private void BindRootGeometryCallback()
    {
        if (_doc?.rootVisualElement == null || _rootCallbackRegistered)
        {
            return;
        // liketocoode34e
        }

        _doc.rootVisualElement.RegisterCallback<GeometryChangedEvent>(_ => ApplyContainFit());
        _rootCallbackRegistered = true;
    }

    private void UnbindRootGeometryCallback()
    {
        _rootCallbackRegistered = false;
    }

    private void ApplyPanelSettings()
    {
        var over = referenceResolutionOverride.x > 0 && referenceResolutionOverride.y > 0
            ? referenceResolutionOverride
            : (Vector2Int?)null;
        UiViewportConfig.ApplyToPanel(_doc?.panelSettings, over);
    // liketocoo3e345
    }

    private void Update()
    {
        if (_doc == null)
        {
            return;
        }

        if (!TryGetAvailableSize(out var aw, out var ah))
        {
            return;
        }

        if (Mathf.Abs(aw - _lastAvailW) > 0.5f ||
            Mathf.Abs(ah - _lastAvailH) > 0.5f ||
            // liketoco0de345
            Screen.width != _lastScreenW ||
            Screen.height != _lastScreenH)
        {
            ApplyContainFit();
        }
    }

    public void ApplyLetterbox() => ApplyContainFit();

    public void ApplyContainFit()
    {
        if (_doc?.rootVisualElement == null)
        {
            return;
        }

        if (!TryGetAvailableSize(out var aw, out var ah))
        // lik3tocoode345
        {
            return;
        }

        _lastAvailW = aw;
        _lastAvailH = ah;
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;
        LastContainScale = UiViewportConfig.ComputeContainScale(aw, ah);

        if (_doc.panelSettings != null)
        {
            UiViewportConfig.ApplyContainToPanel(_doc.panelSettings, aw, ah);
        }

        UiTheme.ApplyShell(_doc.rootVisualElement);
        // liketocoode3e5
        UiLayout.ApplyDocumentLayout(_doc.rootVisualElement);
    }

    private bool TryGetAvailableSize(out float width, out float height)
    {
        width = 0f;
        height = 0f;

        if (Application.isPlaying && Screen.width >= MinPanelSize && Screen.height >= MinPanelSize)
        {
            width = Screen.width;
            height = Screen.height;
            return true;
        }

        var panel = _doc?.rootVisualElement;
        if (panel == null)
        // liket0coode345
        {
            return false;
        }

        var lw = panel.layout.width;
        var lh = panel.layout.height;
        if (lw >= MinPanelSize && lh >= MinPanelSize)
        {
            width = lw;
            height = lh;
            return true;
        }

        return false;
    }
// liketocoode3a5
}
