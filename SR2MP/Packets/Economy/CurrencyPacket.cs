using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class CurrencyPacket : PacketBase
{
    public int NewAmount { get; set; }
    public byte CurrencyType { get; set; }
    public bool ShowUINotification { get; set; }

    public override PacketType Type => PacketType.CurrencyAdjust;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteInt(NewAmount);
        writer.WriteByte(CurrencyType);
        writer.WriteBool(ShowUINotification);
    }

    public override void Deserialise(PacketReader reader)
    {
        NewAmount = reader.ReadInt();
        CurrencyType = reader.ReadByte();
        ShowUINotification = reader.ReadBool();
    }
}