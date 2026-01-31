namespace SR2MP.Networking;

public static class ProtocolConstants
{
    public const int ProtocolVersion = 1; // increment this for new releases
    private const string ProtocolName = "SR2MP";

    public static string ConnectionKey => $"{ProtocolName}:{ProtocolVersion}";
}
