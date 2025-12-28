using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct PediaUnlockPacket : IPacket
{
    public byte Type { get; set; }
    
    public string ID { get; set; }
    public bool Popup { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        
        writer.WriteString(ID);
        writer.WriteBool(Popup);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        
        ID = reader.ReadString();
        Popup = reader.ReadBool();
    }
}