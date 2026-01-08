using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public (string ID, string Username)[] OtherPlayers { get; set; }

    public int Money { get; set; }
    public int RainbowMoney { get; set; }
    public bool AllowCheats { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, PacketWriterDels.Tuple<string, string>.Func);

        writer.WriteInt(Money);
        writer.WriteInt(RainbowMoney);
        writer.WriteBool(AllowCheats);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        OtherPlayers = reader.ReadArray(PacketReaderDels.Tuple<string, string>.Func);
        Money = reader.ReadInt();
        RainbowMoney = reader.ReadInt();
        AllowCheats = reader.ReadBool();
    }
}