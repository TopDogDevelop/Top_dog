namespace TopDog.Client.Tactical;

/// <summary>Selected main-universe combat background for the current realtime battle.</summary>
public static class CombatSpaceBackgroundState
{
    private static string? _activeSetId;
    private static string? _boundBattlefieldId;

    public static string? ActiveSetId => _activeSetId;

    public static void EnsureForBattlefield(string? battlefieldId)
    {
        if (battlefieldId == null)
        {
            return;
        }

        if (battlefieldId.Equals(_boundBattlefieldId, System.StringComparison.Ordinal)
            && !string.IsNullOrEmpty(_activeSetId))
        {
            return;
        }

        _boundBattlefieldId = battlefieldId;
        _activeSetId = CombatBackgroundCatalog.PickRandomMainSetId();
    }

    public static void Reset()
    {
        _activeSetId = null;
        _boundBattlefieldId = null;
    }
}
