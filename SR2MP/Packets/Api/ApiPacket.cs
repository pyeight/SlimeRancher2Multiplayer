using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Api;

internal struct ApiPacket : IPacket
{
    public readonly PacketType Type => PacketType.ApiCall;
    public PacketReliability Reliability { get; }

    public ushort ModId;

    public ApiPacket(PacketReliability reliability, ushort modId)
    {
        Reliability = reliability;
        ModId = modId;
    }

    public void Deserialise(PacketReader reader) => ModId = reader.ReadUShort();

    public readonly void Serialise(PacketWriter writer) => writer.WriteUShort(ModId);
}