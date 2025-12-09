namespace SR2MP.Packets.Utils;

public enum PacketType : byte
{
    Connect = 0,
    ConnectAck = 1,
    Close = 2,
    PlayerJoin = 3,
    PlayerLeave = 4,
    PlayerUpdate = 5,
    Heartbeat = 8,
    HeartbeatAck = 9,
}