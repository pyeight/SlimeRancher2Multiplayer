using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct CurrencyPacket : IPacket
{
    public byte Type { get; set; }
    
    public int Adjust { get; set; }
    public byte CurrencyType { get; set; }
    public bool ShowUINotification { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteInt(Adjust);
        writer.WriteByte(CurrencyType);
        writer.WriteBool(ShowUINotification);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Adjust = reader.ReadInt();
        CurrencyType = reader.ReadByte();
        ShowUINotification = reader.ReadBool();
    }
}