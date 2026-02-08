using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialRefineryPacket : IPacket
{
    public Dictionary<ushort, ushort> Items { get; set; } = new();
    
    public PacketType Type => PacketType.InitialRefinery;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUShort((ushort)Items.Count);
        foreach (var item in Items)
        {
            writer.WriteUShort(item.Key);
            writer.WriteUShort(item.Value);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        Items = new Dictionary<ushort, ushort>();
        ushort count = reader.ReadUShort();
        
        for (int i = 0; i < count; i++)
        {
            ushort key = reader.ReadUShort();
            ushort value = reader.ReadUShort();
            Items.Add(key, value);
        }
    }
}