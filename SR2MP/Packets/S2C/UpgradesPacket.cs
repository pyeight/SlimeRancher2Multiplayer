using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public class UpgradesPacket : IPacket
{
    public byte Type { get; set; }
    
    // I really dislike the idea but i'd rather send a List of integers to the Client than a (shorter) list of strings of string ~ Artur
    public List<int> Upgrades { get; set; }
    
    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteInt(Upgrades.Count);
        foreach (var upgrade in Upgrades)
        {
            writer.WriteInt(upgrade);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        
        var upgradeCount = reader.ReadInt();
        Upgrades = new List<int>(upgradeCount);
        for (var i = 0; i < upgradeCount; i++)
        {
            Upgrades.Add(reader.ReadInt());
        }
    }
}