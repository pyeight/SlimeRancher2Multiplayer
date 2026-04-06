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

        writer.WriteStringWithoutSize(PlayerId);

        writer.WritePackedUInt((uint)OtherPlayers.Length);

        for (var i = 0; i < OtherPlayers.Length; i++)
        {
            var (id, username) = OtherPlayers[i];
            writer.WriteStringWithoutSize(id);
            writer.WriteString(username);
        }

        writer.WritePackedInt(Money);
        writer.WritePackedInt(RainbowMoney);
    }

    public void Deserialise(PacketReader reader)
    {
        InitialJoin = reader.ReadPackedBool();
        AllowCheats = reader.ReadPackedBool();

        PlayerId = reader.ReadPooledStringOfSize(16)!;

        var count = reader.ReadPackedUInt();
        OtherPlayers = new (string ID, string Username)[count];

        for (var i = 0; i < count; i++)
            OtherPlayers[i] = (reader.ReadPooledStringOfSize(16), reader.ReadPooledString())!;

        Money = reader.ReadPackedInt();
        RainbowMoney = reader.ReadPackedInt();
    }
}