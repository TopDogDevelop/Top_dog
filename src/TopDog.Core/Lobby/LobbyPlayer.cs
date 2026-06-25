namespace TopDog.Lobby;

public sealed class LobbyPlayer
{
    public string playerId = Guid.NewGuid().ToString();
    public string displayName = "Player";
    public LobbyPlayerKind kind = LobbyPlayerKind.HUMAN;
    public bool local;
    public bool host;
    public string? remoteHostIp;
    public string? spawnSolarSystemId;
    public string memberTemplateId = "template_1";
    public string assetTemplateId = LobbyCatalogConstants.DefaultTestAssetId;
}
