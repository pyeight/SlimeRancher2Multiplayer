using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class SiloUpdatePacket : IPacket
    {
        public PacketType Type => PacketType.SiloUpdate;

        public string PlotId;
        public int SlotIndex;
        public int ItemTypeId;
        public int Count;

        public SiloUpdatePacket() { }

        public SiloUpdatePacket(string plotId, int slotIndex, int itemTypeId, int count)
        {
            PlotId = plotId;
            SlotIndex = slotIndex;
            ItemTypeId = itemTypeId;
            Count = count;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(PlotId);
            writer.WriteInt(SlotIndex);
            writer.WriteInt(ItemTypeId);
            writer.WriteInt(Count);
        }

        public void Deserialise(PacketReader reader)
        {
            PlotId = reader.ReadString();
            SlotIndex = reader.ReadInt();
            ItemTypeId = reader.ReadInt();
            Count = reader.ReadInt();
        }
    }
}
