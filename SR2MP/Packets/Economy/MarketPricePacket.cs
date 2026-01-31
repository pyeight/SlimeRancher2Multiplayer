using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class MarketPricePacket : PacketBase
{
    public (float Current, float Previous)[] Prices { get; set; }

    public override PacketType Type => PacketType.MarketPriceChange;

    public override void Serialise(PacketWriter writer) => writer.WriteArray(Prices, PacketWriterDels.Tuple<float, float>.Func);

    public override void Deserialise(PacketReader reader) => Prices = reader.ReadArray(PacketReaderDels.Tuple<float, float>.Func);
}