using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class GordoEatPacket : IPacket
    {
        public PacketType Type => PacketType.GordoEat;

        public string Id;
        public int Count;

        public GordoEatPacket() { }

        public GordoEatPacket(string id, int count)
        {
            Id = id;
            Count = count;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(Id);
            writer.WriteInt(Count);
        }

        public void Deserialise(PacketReader reader)
        {
            Id = reader.ReadString();
            Count = reader.ReadInt();
        }
    }
}
