using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class GardenPlantPacket : IPacket
    {
        public PacketType Type => PacketType.GardenPlant;

        public string PlotId;
        public int PlantTypeId; // 0 for empty/harvested
        public int SlotIndex;

        public GardenPlantPacket() { }

        public GardenPlantPacket(string plotId, int plantTypeId, int slotIndex)
        {
            PlotId = plotId;
            PlantTypeId = plantTypeId;
            SlotIndex = slotIndex;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(PlotId);
            writer.WriteInt(PlantTypeId);
            writer.WriteInt(SlotIndex);
        }

        public void Deserialise(PacketReader reader)
        {
            PlotId = reader.ReadString();
            PlantTypeId = reader.ReadInt();
            SlotIndex = reader.ReadInt();
        }
    }
}
