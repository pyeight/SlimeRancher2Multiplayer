using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gadget;

internal sealed class TeleporterLinkPacket : IPacket
{
    public string SourceNodeId = string.Empty;
    public string DestinationNodeId = string.Empty;
    public byte DestinationSceneGroup;

    public PacketType Type => PacketType.TeleporterLink;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

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
