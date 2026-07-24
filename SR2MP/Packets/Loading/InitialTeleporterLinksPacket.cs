using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialTeleporterLinksPacket : IPacket
{
    internal sealed class Link : INetObject
    {
        public string SourceNodeId = string.Empty;
        public string DestinationNodeId = string.Empty;
        public byte DestinationSceneGroup;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(SourceNodeId);
            writer.WriteString(DestinationNodeId);
            writer.WriteByte(DestinationSceneGroup);
        }

        public void Deserialise(PacketReader reader)
        {
            SourceNodeId = reader.ReadPooledString()!;
            DestinationNodeId = reader.ReadPooledString()!;
            DestinationSceneGroup = reader.ReadByte();
        }
    }

    public List<Link> Links;

    public PacketType Type => PacketType.InitialTeleporterLinks;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Links, PacketWriterDels.NetObject<Link>.Writer);

    public void Deserialise(PacketReader reader) => Links = reader.ReadList(PacketReaderDels.NetObject<Link>.Reader)!;
}