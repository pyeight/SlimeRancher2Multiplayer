using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

public struct CurrencyPacket : IPacket
{
    public int NewAmount;
    public byte CurrencyType;
    public bool ShowUINotification;

    public readonly PacketType Type => PacketType.CurrencyAdjust;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteInt(NewAmount);
        writer.WriteByte(CurrencyType);
        writer.WriteBool(ShowUINotification);
    }

    public void Deserialise(PacketReader reader)
    {
        NewAmount = reader.ReadInt();
        CurrencyType = reader.ReadByte();
        ShowUINotification = reader.ReadBool();
    }
}