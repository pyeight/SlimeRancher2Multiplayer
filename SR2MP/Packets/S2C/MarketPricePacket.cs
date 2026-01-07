using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct MarketPricePacket : IPacket
{
    public byte Type { get; set; }
    
    public float[] Prices { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteArray(Prices, PacketWriterDels.Float);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Prices = reader.ReadArray(PacketReaderDels.Float);
    }
}