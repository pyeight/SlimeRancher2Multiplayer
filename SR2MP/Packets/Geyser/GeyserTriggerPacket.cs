using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Geyser;

public struct GeyserTriggerPacket : IPacket
{
    public byte Type { get; set; }

    // Couldnt find an ID system for these so I need to access them through GameObject.Find
    public string ObjectPath { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(ObjectPath);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ObjectPath = reader.ReadString();
    }
}