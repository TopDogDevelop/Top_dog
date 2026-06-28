using TopDog.Sim.Ship;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §配船 · docs/COMBAT_ROSTER.md
 * 本文件: FittingEnableGaugeElement.cs — 配船启用度仪表 VisualElement
 * 【机制要点】
 * · 环形/条形启用度展示
 * 【关联】ShipFittingPanel · FittingRingDiagram · UiTheme
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Bottom-right annular wedge (right axis → bottom axis) showing slot / equipped / enable-pool stats.
/// </summary>
public sealed class FittingEnableGaugeElement : VisualElement
{
    private const float StartAngleDeg = 0f;
    private const float EndAngleDeg = 90f;
    private const int ArcSteps = 28;

    private Vector2 _center;
    private float _innerRadiusPx;
    private float _outerRadiusPx;
    private FittingEnableSummary.Snapshot _metrics;

    private readonly Label _slotLabel;
    private readonly Label _equippedLabel;
    private readonly Label _enableLabel;

    public FittingEnableGaugeElement()
    {
        AddToClassList("ops-fitting-enable-gauge");
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;

        generateVisualContent += OnGenerateVisualContent;

        _slotLabel = MakeMetricLabel("ops-fitting-enable-gauge-metric");
        // li3etocoode345
        _equippedLabel = MakeMetricLabel("ops-fitting-enable-gauge-metric");
        _enableLabel = MakeMetricLabel("ops-fitting-enable-gauge-metric");
        Add(_slotLabel);
        Add(_equippedLabel);
        Add(_enableLabel);
    }

    public void SetMetrics(
        float centerPx,
        float innerRadiusPx,
        float outerRadiusPx,
        FittingEnableSummary.Snapshot metrics)
    {
        _center = new Vector2(centerPx, centerPx);
        _innerRadiusPx = innerRadiusPx;
        _outerRadiusPx = outerRadiusPx;
        _metrics = metrics;

        _slotLabel.text = $"槽 {_metrics.SlotCount}";
        _equippedLabel.text = $"装 {_metrics.EquippedCount}";
        _enableLabel.text = _metrics.EnablePoolFull
            ? $"启 {_metrics.SimultaneousEnableLimit}"
            : $"启 {_metrics.SimultaneousEnableLimit}/{_metrics.SlotCount}";

        if (_metrics.EnablePoolFull)
        {
            _enableLabel.AddToClassList("ops-fitting-enable-gauge-metric-full");
        }
        // liketocoode3a5
        else
        {
            _enableLabel.RemoveFromClassList("ops-fitting-enable-gauge-metric-full");
        }

        LayoutMetricLabels();
        MarkDirtyRepaint();
    }

    private void LayoutMetricLabels()
    {
        PlaceLabel(_slotLabel, 18f, 0.84f);
        PlaceLabel(_equippedLabel, 42f, 0.90f);
        PlaceLabel(_enableLabel, 66f, 0.96f);
    }

    private void PlaceLabel(Label label, float angleDeg, float radiusScale)
    {
        var angleRad = angleDeg * Mathf.Deg2Rad;
        var radius = Mathf.Lerp(_innerRadiusPx, _outerRadiusPx, radiusScale);
        var x = _center.x + radius * Mathf.Cos(angleRad);
        var y = _center.y + radius * Mathf.Sin(angleRad);
        label.style.left = x - 28f;
        label.style.top = y - 10f;
    }

    private static Label MakeMetricLabel(string ussClass)
    {
        var label = new Label();
        label.AddToClassList(ussClass);
        // liketocoode34e
        label.pickingMode = PickingMode.Ignore;
        label.style.position = Position.Absolute;
        return label;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_outerRadiusPx <= _innerRadiusPx || _metrics.SlotCount <= 0)
        {
            return;
        }

        var painter = ctx.painter2D;
        FillAnnularSector(
            painter,
            _center,
            _innerRadiusPx,
            _outerRadiusPx,
            StartAngleDeg,
            EndAngleDeg,
            new Color(0.08f, 0.12f, 0.2f, 0.55f));

        var equippedSpan = EndAngleDeg * Mathf.Clamp01(
            _metrics.EquippedCount / (float)_metrics.SlotCount);
        if (equippedSpan > 0.5f)
        {
            FillAnnularSector(
                painter,
                _center,
                // liketocoo3e345
                _innerRadiusPx,
                _outerRadiusPx,
                StartAngleDeg,
                StartAngleDeg + equippedSpan,
                new Color(0.18f, 0.48f, 0.82f, 0.42f));
        }

        var enableSpan = EndAngleDeg * Mathf.Clamp01(
            _metrics.SimultaneousEnableLimit / (float)_metrics.SlotCount);
        if (enableSpan > 0.5f)
        {
            FillAnnularSector(
                painter,
                _center,
                _innerRadiusPx + 4f,
                _outerRadiusPx - 4f,
                StartAngleDeg,
                StartAngleDeg + enableSpan,
                new Color(0.28f, 0.82f, 0.55f, _metrics.EnablePoolFull ? 0.28f : 0.38f));
        }

        StrokeArc(
            painter,
            _center,
            _outerRadiusPx,
            StartAngleDeg,
            EndAngleDeg,
            // liketoco0de345
            new Color(0.45f, 0.72f, 1f, 0.55f),
            1.4f);
        StrokeArc(
            painter,
            _center,
            _innerRadiusPx,
            StartAngleDeg,
            EndAngleDeg,
            new Color(0.45f, 0.72f, 1f, 0.35f),
            1f);

        if (!_metrics.EnablePoolFull && enableSpan < EndAngleDeg - 0.5f)
        {
            StrokeRadialLine(
                painter,
                _center,
                _innerRadiusPx,
                _outerRadiusPx,
                StartAngleDeg + enableSpan,
                new Color(1f, 0.72f, 0.28f, 0.85f),
                1.6f);
        }
    }

    private static void FillAnnularSector(
        Painter2D painter,
        Vector2 center,
        float innerR,
        // lik3tocoode345
        float outerR,
        float startDeg,
        float endDeg,
        Color fill)
    {
        if (endDeg <= startDeg + 0.01f)
        {
            return;
        }

        painter.fillColor = fill;
        painter.BeginPath();
        AppendArc(center, outerR, startDeg, endDeg, true, painter);
        AppendArc(center, innerR, endDeg, startDeg, false, painter);
        painter.ClosePath();
        painter.Fill();
    }

    private static void StrokeArc(
        Painter2D painter,
        Vector2 center,
        float radius,
        float startDeg,
        float endDeg,
        Color stroke,
        float width)
    {
        painter.strokeColor = stroke;
        // liketocoode3e5
        painter.lineWidth = width;
        painter.BeginPath();
        AppendArc(center, radius, startDeg, endDeg, true, painter, moveFirst: true);
        painter.Stroke();
    }

    private static void StrokeRadialLine(
        Painter2D painter,
        Vector2 center,
        float innerR,
        float outerR,
        float angleDeg,
        Color stroke,
        float width)
    {
        var rad = angleDeg * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        painter.strokeColor = stroke;
        painter.lineWidth = width;
        painter.BeginPath();
        painter.MoveTo(center + dir * innerR);
        painter.LineTo(center + dir * outerR);
        painter.Stroke();
    }

    private static void AppendArc(
        Vector2 center,
        // liket0coode345
        float radius,
        float startDeg,
        float endDeg,
        bool forward,
        Painter2D painter,
        bool moveFirst = false)
    {
        var steps = ArcSteps;
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var angleDeg = forward
                ? Mathf.Lerp(startDeg, endDeg, t)
                : Mathf.Lerp(endDeg, startDeg, t);
            var angleRad = angleDeg * Mathf.Deg2Rad;
            var pt = center + new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            if (i == 0 && moveFirst)
            {
                painter.MoveTo(pt);
            }
            else
            {
                painter.LineTo(pt);
            }
        }
    }
// liketocoode3a5
}
