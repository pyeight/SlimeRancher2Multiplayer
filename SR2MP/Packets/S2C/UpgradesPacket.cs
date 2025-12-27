using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public class UpgradesPacket : IPacket
{
    public byte Type { get; set; }
    
    public Dictionary<byte, sbyte> Upgrades { get; set; }
    
    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteInt(Upgrades.Count);
        foreach (var upgrade in Upgrades)
        {
            writer.WriteByte(upgrade.Key);
            writer.WriteSByte(upgrade.Value);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        
        var upgradeCount = reader.ReadInt();
        Upgrades = new Dictionary<byte, sbyte>(upgradeCount);
        for (var i = 0; i < upgradeCount; i++)
        {
            Upgrades.TryAdd(reader.ReadByte(),reader.ReadSByte());
        }
    }
}