using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class ConnectionApprovePacket : IPacket
{
    public bool InitialJoin;

    public string PlayerId;
    public (string ID, string Username)[] OtherPlayers;

    public int Money;
    public int RainbowMoney;
    public bool AllowCheats;

    public PacketType Type => PacketType.ConnectionApprove;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedBool(InitialJoin);
        writer.WritePackedBool(AllowCheats);

        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, PacketWriterDels.Tuple<string, string>.Func);

        writer.WritePackedInt(Money);
        writer.WritePackedInt(RainbowMoney);
    }

    public void Deserialise(PacketReader reader)
    {
        InitialJoin = reader.ReadPackedBool();
        AllowCheats = reader.ReadPackedBool();

        PlayerId = reader.ReadString()!;
        OtherPlayers = reader.ReadArray(PacketReaderDels.Tuple<string, string>.Func);

        Money = reader.ReadPackedInt();
        RainbowMoney = reader.ReadPackedInt();
    }
}