using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class TreasurePodPacket : IPacket
    {
        public PacketType Type => PacketType.TreasurePod;

        public string Id;

        public TreasurePodPacket() { }

        public TreasurePodPacket(string id)
        {
            Id = id;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(Id);
        }

        public void Deserialise(PacketReader reader)
        {
            Id = reader.ReadString();
        }
    }
}
