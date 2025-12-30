using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class SaveDataPacket : IPacket
    {
        public PacketType Type => PacketType.SaveData;

        public int Length;
        public byte[] Data;

        public SaveDataPacket() { }

        public SaveDataPacket(byte[] data)
        {
            Data = data;
            Length = data.Length;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteInt(Length);
            writer.WriteBytes(Data);
        }

        public void Deserialise(PacketReader reader)
        {
            Length = reader.ReadInt();
            Data = reader.ReadBytes(Length);
        }
    }
}
