using SR2MP.Packets.Utils;

namespace SR2MP.Packets.TreasurePod;

internal sealed class InitialTreasurePodsPacket : IPacket
{
    public Dictionary<int, Il2Cpp.TreasurePod.State> TreasurePods;

    public PacketType Type => PacketType.InitialTreasurePods;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) =>
        writer.WriteDictionary(
            TreasurePods,
            PacketWriterDels.PackedInt32,
            PacketWriterDels.PackedEnum<Il2Cpp.TreasurePod.State>.Writer
        );

    public void Deserialise(PacketReader reader) =>
        TreasurePods = reader.ReadDictionary(
            PacketReaderDels.PackedInt32,
            PacketReaderDels.PackedEnum<Il2Cpp.TreasurePod.State>.Reader
        )!;
}