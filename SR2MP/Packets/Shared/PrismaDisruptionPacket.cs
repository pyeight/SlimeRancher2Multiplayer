using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class PrismaDisruptionPacket : IPacket
    {
        public PacketType Type => PacketType.PrismaDisruption;

        public string AreaId;     // Name of DisruptionAreaDefinition
        public int Level;       // Cast of DisruptionLevel
        public bool IsTransition;

        public PrismaDisruptionPacket() { }

        public PrismaDisruptionPacket(string areaId, int level, bool isTransition)
        {
            AreaId = areaId;
            Level = level;
            IsTransition = isTransition;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(AreaId);
            writer.WriteInt(Level);
            writer.WriteBool(IsTransition);
        }

        public void Deserialise(PacketReader reader)
        {
            AreaId = reader.ReadString();
            Level = reader.ReadInt();
            IsTransition = reader.ReadBool();
        }
    }
}
