using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.Control)]
public sealed class ConnectAckPacket : PacketBase
{
    public string PlayerId { get; set; }
    public (string ID, string Username)[] OtherPlayers { get; set; }

    public int Money { get; set; }
    public int RainbowMoney { get; set; }
    public bool AllowCheats { get; set; }

    public override PacketType Type => PacketType.ConnectAck;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, PacketWriterDels.Tuple<string, string>.Func);

        writer.WriteInt(Money);
        writer.WriteInt(RainbowMoney);
        writer.WriteBool(AllowCheats);
    }

    public override void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        OtherPlayers = reader.ReadArray(PacketReaderDels.Tuple<string, string>.Func);
        Money = reader.ReadInt();
        RainbowMoney = reader.ReadInt();
        AllowCheats = reader.ReadBool();
    }
}