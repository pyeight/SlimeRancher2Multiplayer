using SR2E.Utils;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public class PlayerAmmoSlot
{
    public int Count { get; init; }
    public string ItemName { get; init; }
}

public sealed class PlayerInventoryPacket : IPacket
{
    public PacketType Type { get; init; }
    public string PlayerId { get; set; }
    public List<PlayerAmmoSlot> Slots { get; set; }


    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteInt(Slots.Count);
        foreach (var slot in Slots)
        {
            writer.WriteInt(slot.Count);
            writer.WriteString(slot.ItemName);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        var slotCount = reader.ReadInt();
        Slots = new List<PlayerAmmoSlot>(slotCount);
        for (var i = 0; i < slotCount; i++)
        {
            Slots.Add(new PlayerAmmoSlot
            {
                Count = reader.ReadInt(),
                ItemName = reader.ReadString()
            });
        }
    }
}