using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

internal sealed class InitialTreasurePodsPacket : IPacket
{
    internal sealed class Pod : INetObject
    {
        public string ID;
        public Il2Cpp.TreasurePod.State State;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WritePackedEnum(State);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadPooledString()!;
            State = reader.ReadPackedEnum<Il2Cpp.TreasurePod.State>();
        }
    }

    public List<Pod> TreasurePods;

    public PacketType Type => PacketType.InitialTreasurePods;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(TreasurePods, PacketWriterDels.NetObject<Pod>.Writer);

    public void Deserialise(PacketReader reader) => TreasurePods = reader.ReadList(PacketReaderDels.NetObject<Pod>.Reader)!;
}