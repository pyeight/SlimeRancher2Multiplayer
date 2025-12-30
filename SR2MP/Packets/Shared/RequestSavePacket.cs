using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class RequestSavePacket : IPacket
    {
        public PacketType Type => PacketType.RequestSave;

        public RequestSavePacket() { }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
        }

        public void Deserialise(PacketReader reader)
        {
        }
    }
}
