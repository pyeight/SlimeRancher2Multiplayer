namespace SR2MP.Networking;

public static class NetChannels
{
    public const byte Control = 0;
    public const byte EntityPositions = 1;
    public const byte WorldState = 2;
    public const byte Fx = 3;

    // Number of channels for LiteNetLib to allocate
    // We have 4 (Control, PlayerState, WorldState, Fx)
    // If we add more we need to increase
    public const byte Count = 4;
}
