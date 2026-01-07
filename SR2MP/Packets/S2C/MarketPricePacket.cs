using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public sealed class MarketPricePacket : IPacket
{
    public byte Type { get; set; }
    
    public (float Current, float Previous)[] Prices { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteArray(Prices, (packetWriter, tuple) =>
        {
            packetWriter.WriteFloat(tuple.Current);
            packetWriter.WriteFloat(tuple.Previous);
        });
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Prices = reader.ReadArray((packetReader) => (packetReader.ReadFloat(), packetReader.ReadFloat()));
    }
}