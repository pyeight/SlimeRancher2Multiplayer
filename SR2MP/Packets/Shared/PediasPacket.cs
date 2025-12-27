using System.Diagnostics;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Utils;
using Il2CppSystem.Linq;

namespace SR2MP.Packets.Shared;

public struct PediasPacket : IPacket
{
    public byte Type { get; set; }
    
    public List<string> Entries { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        
        writer.WriteInt(Entries.Count);
        foreach (var entry in Entries)
            writer.WriteString(entry);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        
        var entryCount = reader.ReadInt();
        Entries = new List<string>(entryCount);
        for (int i = 0; i < entryCount; i++)
            Entries.Add(reader.ReadString());
    }
}