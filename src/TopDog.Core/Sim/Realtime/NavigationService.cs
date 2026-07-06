/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §4 NAVIGATE
 * 本文件: NavigationService.cs — 持续接近导航点
 * 【关联】FleetOrderService · BattlefieldSystem
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class NavigationService
{
    public static void AssignNavigate(BattlefieldUnit u, float x, float y, float z)
    {
        LogisticsAutoTargetingService.SuppressForPlayerOrder(u);
        u.aiOrder = UnitAiOrder.NAVIGATE;
        u.navigateX = x;
        u.navigateY = y;
        u.navigateZ = z;
        u.approachTargetUnitId = null;
        u.orbitTargetUnitId = null;
        u.throttleOn = true;
    }

}
