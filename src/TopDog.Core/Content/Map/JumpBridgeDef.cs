namespace TopDog.Content.Map;

public sealed class JumpBridgeDef
{
    public string? bridgeId;
    public string? fromSystemId;
    public string? toSystemId;
    public string? garrisonTemplateId;

    public JumpBridgeDef Copy()
    {
        return new JumpBridgeDef
        {
            bridgeId = bridgeId,
            fromSystemId = fromSystemId,
            toSystemId = toSystemId,
            garrisonTemplateId = garrisonTemplateId,
        };
    }
}
