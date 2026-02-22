using System.Net;

namespace SR2MP.Shared.Utils;

public readonly struct PacketKey : IEquatable<PacketKey>
{
    public readonly IPEndPoint EndPoint;
    public readonly ushort PacketId;
    public readonly byte PacketType;

    public PacketKey(byte packetType, ushort packetId, IPEndPoint endPoint)
    {
        PacketType = packetType;
        PacketId = packetId;
        EndPoint = endPoint;
    }

    public bool Equals(PacketKey other) =>
        PacketType == other.PacketType &&
        PacketId == other.PacketId &&
        Equals(EndPoint, other.EndPoint);

    public override bool Equals(object? obj) => obj is PacketKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(PacketType, PacketId, EndPoint);
}