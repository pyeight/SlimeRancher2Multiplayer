using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

public class InitialTreasurePodsPacket : IPacket
{
    public Dictionary<int, Il2Cpp.TreasurePod.State> TreasurePods = new();

    public PacketType Type => PacketType.InitialTreasurePods;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) =>
        writer.WriteDictionary<int, Il2Cpp.TreasurePod.State>(
            TreasurePods,
            PacketWriterDels.Int32,
            PacketWriterDels.Enum<Il2Cpp.TreasurePod.State>.Func
        );

    public void Deserialise(PacketReader reader) =>
        TreasurePods = reader.ReadDictionary<int, Il2Cpp.TreasurePod.State>(
            PacketReaderDels.Int32,
            PacketReaderDels.Enum<Il2Cpp.TreasurePod.State>.Func
        );
}