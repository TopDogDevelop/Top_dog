/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/FLEET_SCALE_10K.md §2 · docs/VISUAL_ASSETS.md §3
 * 本文件: TacticalIconGpuLayer.cs — 面板批画入口 + 密舰队相机 Fit
 * 【机制要点】
 * · 舰标一律面板批画（与机制测编号无关）；密舰队门槛只影响 tick/相机 Fit，不另开一套渲染
 * · FitCameraToBattlefield：ResetToTopDown + ZoomOut（仅密舰队调用）
 * 【关联】TacticalIconBatchElement · TacticalViewportPresenter · BattlefieldScalePolicy
 * ══
 */

using TopDog.Sim.Realtime;
using UnityEngine;

namespace TopDog.Client.Tactical;

public static class TacticalIconGpuLayer
{
    public const int GpuIconUnitThreshold = BattlefieldScalePolicy.DenseUnitThreshold;

    /// <summary>任意战场统一批画舰标；规模门控不再切换渲染路径。</summary>
    public static bool ShouldUseGpuPath(BattlefieldState? bf) => bf != null;

    public static void FitCameraToBattlefield(TacticalViewportCamera cam, BattlefieldState bf)
    {
        cam?.ResetToTopDown(bf);
        if (cam == null || bf == null)
        {
            return;
        }

        var maxR = 0f;
        foreach (var u in bf.units)
        {
            if (u == null || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            var r = u.x * u.x + u.y * u.y + u.z * u.z;
            if (r > maxR)
            {
                maxR = r;
            }
        }

        if (maxR <= 0f)
        {
            return;
        }

        var need = Mathf.Sqrt(maxR) * 2.2f;
        while (cam.ViewDistance < need && cam.ViewDistance < 600_000f)
        {
            cam.ZoomOut();
        }
    }
}
