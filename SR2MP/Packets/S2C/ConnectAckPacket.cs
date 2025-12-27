using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public sealed class ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public string[] OtherPlayers { get; set; }

    public int Money { get; set; }
    public int RainbowMoney { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, PacketWriterDels.String);

        writer.WriteInt(Money);
        writer.WriteInt(RainbowMoney);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        OtherPlayers = reader.ReadArray(PacketReaderDels.String);

        Money = reader.ReadInt();
        RainbowMoney = reader.ReadInt();
    }
}