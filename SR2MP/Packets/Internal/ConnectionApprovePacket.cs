using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

internal sealed class ConnectionApprovePacket : IPacket
{
    public bool InitialJoin;

    public string PlayerId;
    public (string ID, string Username)[] OtherPlayers;

    public int Money;
    public int RainbowMoney;
    public bool AllowCheats;

    public List<ModNetData> NetData;

    public PacketType Type => PacketType.ConnectionApprove;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Important;

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

        writer.WriteList(NetData, PacketWriterDels.NetObject<ModNetData>.Writer);
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

        NetData = reader.ReadList(PacketReaderDels.NetObject<ModNetData>.Reader)!;
    }
}

internal struct ModNetData : INetObject
{
    public uint ModId;
    public byte NetId;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteUInt(ModId);
        writer.WriteByte(NetId);
    }

    public void Deserialise(PacketReader reader)
    {
        ModId = reader.ReadUInt();
        NetId = reader.ReadByte();
    }
}