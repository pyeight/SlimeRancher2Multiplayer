using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class PrismaBarrierPacket : IPacket
    {
        public PacketType Type => PacketType.PrismaBarrier;

        public string BarrierId;
        public double ActivationTime;

        public PrismaBarrierPacket() { }

        public PrismaBarrierPacket(string barrierId, double activationTime)
        {
            BarrierId = barrierId;
            ActivationTime = activationTime;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(BarrierId);
            writer.WriteDouble(ActivationTime);
        }

        public void Deserialise(PacketReader reader)
        {
            BarrierId = reader.ReadString();
            ActivationTime = reader.ReadDouble();
        }
    }
}
