using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct InventoryPacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public int SlotIdx { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteInt(SlotIdx);
        writer.WriteInt(ItemId);
        writer.WriteInt(Count);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        SlotIdx = reader.ReadInt();
        ItemId = reader.ReadInt();
        Count = reader.ReadInt();
    }
}
