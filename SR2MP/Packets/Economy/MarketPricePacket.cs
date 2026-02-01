using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

public sealed class MarketPricePacket : IPacket
{
    public (float Current, float Previous)[] Prices { get; set; }

    public PacketType Type => PacketType.MarketPriceChange;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteArray(Prices, PacketWriterDels.Tuple<float, float>.Func);

    public void Deserialise(PacketReader reader) => Prices = reader.ReadArray(PacketReaderDels.Tuple<float, float>.Func);
}