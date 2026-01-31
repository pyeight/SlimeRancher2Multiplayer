using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class InitialPediaPacket : PacketBase
{
    public List<string> Entries { get; set; }

    public override PacketType Type => PacketType.InitialPediaEntries;

    public override void Serialise(PacketWriter writer) => writer.WriteList(Entries, PacketWriterDels.String);

    public override void Deserialise(PacketReader reader) => Entries = reader.ReadList(PacketReaderDels.String);
}