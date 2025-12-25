using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public struct ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public string[] OtherPlayers { get; set; }
    
    public int Money { get; set; }
    public int RainbowMoney { get; set; }
    
    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteArray(OtherPlayers, (writer, val) => { writer.WriteString(val); });
        
        writer.WriteInt(Money);
        writer.WriteInt(RainbowMoney);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        OtherPlayers = reader.ReadArray((reader) => reader.ReadString());
        
        Money = reader.ReadInt();
        RainbowMoney = reader.ReadInt();
    }
}