using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>舰船详情 HUD 分区矩形（中心原点 · content/ui/combat_ship_detail_hud.template.json）。</summary>
public static class CombatShipDetailHudLayout
{
    public const int MarkerCenterPx = 16;

    public readonly struct Region
    {
        public readonly string Id;
        public readonly int X0;
        public readonly int Y0;
        public readonly int W;
        public readonly int H;

        public Region(string id, int x0, int y0, int w, int h)
        {
            Id = id;
            X0 = x0;
            Y0 = y0;
            W = w;
            H = h;
        }
    }

    public static readonly Region Shield = new("shield_bar", -26, -50, 60, 5);
    public static readonly Region Armor = new("armor_bar", -26, -45, 60, 5);
    public static readonly Region Structure = new("structure_bar", -26, -40, 68, 5);
    public static readonly Region BuffRail = new("buff_rail", -40, -35, 14, 71);
    public static readonly Region Capacitor = new("capacitor_bar", 26, -40, 7, 51);
    public static readonly Region Implant = new("implant_bar", 33, -40, 8, 75);
    public static readonly Region NameLegion = new("name_legion_row", -40, 16, 82, 19);
    public static readonly Region Speed = new("speed_panel", -30, 35, 61, 20);

    public static void Place(VisualElement? el, in Region region)
    {
        if (el == null)
        {
            return;
        }

        el.style.position = Position.Absolute;
        el.style.left = region.X0;
        el.style.top = region.Y0;
        el.style.width = region.W;
        el.style.height = region.H;
        el.style.marginLeft = 0;
        el.style.marginTop = 0;
        el.style.marginRight = 0;
        el.style.marginBottom = 0;
    }

    public static void PlaceVerticalBar(VisualElement? el, in Region region)
    {
        Place(el, region);
        if (el == null)
        {
            return;
        }

        el.AddToClassList("rtcombat-ship-detail-bar-vertical");
    }

    public static void PlaceHorizontalBar(VisualElement? el, in Region region)
    {
        Place(el, region);
        if (el == null)
        {
            return;
        }

        el.AddToClassList("rtcombat-ship-detail-bar-horizontal");
    }
}
