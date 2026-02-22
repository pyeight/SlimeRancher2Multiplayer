using System.Net;

namespace SR2MP.Shared.Utils;

public readonly struct SequenceKey : IEquatable<SequenceKey>
{
    public readonly IPEndPoint Endpoint;
    public readonly byte PacketType;

    public SequenceKey(IPEndPoint endpoint, byte packetType)
    {
        Endpoint = endpoint;
        PacketType = packetType;
    }

    public bool Equals(SequenceKey other) => Endpoint.Equals(other.Endpoint) && PacketType == other.PacketType;

    public override bool Equals(object? obj) => obj is SequenceKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Endpoint, PacketType);
}