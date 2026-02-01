namespace SR2MP.Packets.Utils;

public enum PacketReliability : byte
{
    Unreliable = 0,
    Reliable = 1,
    ReliableOrdered = 2
}