using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class ConnectAckPacket : IPacket
{
    public string PlayerId;
    public (string ID, string Username)[] OtherPlayers;

    public int Money;
    public int RainbowMoney;
    public bool AllowCheats;

    public PacketType Type => PacketType.ConnectAck;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, PacketWriterDels.Tuple<string, string>.Func);

        writer.WritePackedInt(Money);
        writer.WritePackedInt(RainbowMoney);
        writer.WriteBool(AllowCheats);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        OtherPlayers = reader.ReadArray(PacketReaderDels.Tuple<string, string>.Func);
        Money = reader.ReadPackedInt();
        RainbowMoney = reader.ReadPackedInt();
        AllowCheats = reader.ReadBool();
    }
}