using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Api;

internal struct ApiPacket : IPacket
{
    public readonly PacketType Type => PacketType.ApiCall;
    public PacketReliability Reliability { get; }
    public NetworkChannel Channel { get; }

    public byte NetId;

    public ApiPacket(PacketReliability reliability, NetworkChannel channel, byte netId)
    {
        Reliability = reliability;
        Channel = channel;
        NetId = netId;
    }

    public void Deserialise(PacketReader reader) => NetId = reader.ReadByte();

    public readonly void Serialise(PacketWriter writer) => writer.WriteByte(NetId);
}