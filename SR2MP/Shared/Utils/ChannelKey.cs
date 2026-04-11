using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Utils;

/// <summary>
/// A key used to track sequence numbers and reorder buffers per packet type, channel and endpoint.
/// Ordering per packet type in a channel, different ordered packet types on the same channel won't collide.
/// </summary>
internal readonly struct ChannelKey : IEquatable<ChannelKey>
{
    private readonly IPEndPoint EndPoint;
    private readonly NetworkChannel Channel;
    private readonly byte PacketType;

    public ChannelKey(IPEndPoint endPoint, NetworkChannel channel, byte packetType)
    {
        EndPoint = endPoint;
        Channel = channel;
        PacketType = packetType;
    }

    public bool Equals(ChannelKey other)
        => PacketType == other.PacketType && Channel == other.Channel && EndPoint.Equals(other.EndPoint);

    public override bool Equals(object? obj)
        => obj is ChannelKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(EndPoint, Channel, PacketType);

    public override string ToString()
        => $"{EndPoint}:{Channel}:{PacketType}";
}