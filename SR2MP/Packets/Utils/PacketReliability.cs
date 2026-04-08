namespace SR2MP.Packets.Utils;

public enum PacketReliability : byte
{
    Unreliable = 0,
    UnreliableOrdered = 1,
    Reliable = 2,
    ReliableOrdered = 3
}