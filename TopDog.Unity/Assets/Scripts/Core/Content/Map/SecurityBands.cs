namespace TopDog.Content.Map;

public sealed class SecurityBands
{
    public List<Band> bands = new();

    public sealed class Band
    {
        public string? id;
        public float minSecurity;
        public float maxSecurity;
        public string? uiColor;
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
