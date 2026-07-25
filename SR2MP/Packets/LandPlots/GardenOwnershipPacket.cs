using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class GardenOwnershipPacket : IPacket
{
    internal struct Entry : INetObject
    {
        public string GardenId;
        public string OwnerId;

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteString(GardenId);
            writer.WriteString(OwnerId);
        }

        public void Deserialise(PacketReader reader)
        {
            GardenId = reader.ReadPooledString()!;
            OwnerId = reader.ReadPooledString() ?? string.Empty;
        }
    }

    public List<Entry> Entries;

    public PacketType Type => PacketType.GardenOwnership;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Landplots;

    public void Serialise(PacketWriter writer)
        => writer.WriteList(Entries, PacketWriterDels.NetObject<Entry>.Writer);

    public void Deserialise(PacketReader reader)
        => Entries = reader.ReadList(PacketReaderDels.NetObject<Entry>.Reader)!;
}