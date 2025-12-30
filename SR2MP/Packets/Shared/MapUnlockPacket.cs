using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class MapUnlockPacket : IPacket
    {
        public PacketType Type => PacketType.MapUnlock;

        public string Id;

        public MapUnlockPacket() { }

        public MapUnlockPacket(string id)
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
