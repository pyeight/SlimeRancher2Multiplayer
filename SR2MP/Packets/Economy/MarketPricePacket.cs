using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

public sealed class MarketPricePacket : IPacket
{
    public byte Type { get; set; }

    public (float Current, float Previous)[] Prices { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteArray(Prices, PacketWriterDels.Tuple<float, float>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Prices = reader.ReadArray(PacketReaderDels.Tuple<float, float>.Func);
    }
}