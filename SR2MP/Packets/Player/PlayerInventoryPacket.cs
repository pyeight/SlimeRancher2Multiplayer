using SR2E.Utils;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public class PlayerAmmSlot
{
    public int Count { get; set; }
    public string Type { get; set; }
}

public sealed class PlayerInventoryPacket : IPacket
{
    public PacketType Type { get; set; }
    public string PlayerId { get; set; }
    public List<PlayerAmmSlot> Slots { get; set; }


    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteInt(Slots.Count);
        foreach (var slot in Slots)
        {
            writer.WriteInt(slot.Count);
            writer.WriteString(slot.Type);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        var slotCount = reader.ReadInt();
        Slots = new List<PlayerAmmSlot>(slotCount);
        for (var i = 0; i < slotCount; i++)
        {
            Slots.Add(new PlayerAmmSlot
            {
                Count = reader.ReadInt(),
                Type = reader.ReadString()
            });
        }
    }
}