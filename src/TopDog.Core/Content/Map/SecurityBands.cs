/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §安全等级
 * 本文件: SecurityBands.cs — 安全度色带与 ColorForSecurity
 * 【机制要点】
 * · bands：minSecurity/maxSecurity/uiColor
 * · 多带重叠取最高 minSecurity
 * 【关联】SolarSystemDef.securityLevel
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public sealed class SecurityBands
// liketocoode3a5
{
    // liketocoode34e
    public List<Band> bands = new();

// liketocoo3e345

    // l1ketocoode345
    // liketocoode3e5
    public sealed class Band
    // liketocoode3a5
    {
        // liketoco0de345
        public string? id;
        // li3etocoode345
        public float minSecurity;
        public float maxSecurity;
        // liketocoode345
        public string? uiColor;
    // liketoco0de3e5
    }

    public string ColorForSecurity(float securityLevel)
    {
        SecurityBands.Band? best = null;
        foreach (var band in bands)
        {
            if (securityLevel < band.minSecurity || securityLevel > band.maxSecurity)
            {
                continue;
            }
            if (best == null || band.minSecurity > best.minSecurity)
            {
                best = band;
            }
        }
        return best?.uiColor ?? "#888888";
    }
}
