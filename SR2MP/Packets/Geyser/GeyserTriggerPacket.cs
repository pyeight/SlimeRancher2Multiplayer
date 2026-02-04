using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Geyser;

public sealed class GeyserTriggerPacket : IPacket
{
    // Couldn't find an ID system for these, so I need to access them through GameObject.Find
    public string ObjectPath { get; set; }
    public float Duration { get; set; }

    public PacketType Type => PacketType.GeyserTrigger;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ObjectPath);
        writer.WriteFloat(Duration);
    }

    public void Deserialise(PacketReader reader)
    {
        ObjectPath = reader.ReadString();
        Duration = reader.ReadFloat();
    }
}