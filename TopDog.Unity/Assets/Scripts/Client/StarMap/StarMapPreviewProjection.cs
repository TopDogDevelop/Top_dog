namespace TopDog.Client.StarMap;

/// <summary>2D orthographic projections for lobby star-map preview (engineering three-view).</summary>
public enum StarMapPreviewProjection
{
    /// <summary>Top-down: horizontal X, vertical −Z (default strategic map).</summary>
    TopDownXz,

    /// <summary>Side elevation: horizontal X, vertical −Y.</summary>
    SideXy,

    /// <summary>Front elevation: horizontal Z, vertical −Y.</summary>
    FrontYz,
}
