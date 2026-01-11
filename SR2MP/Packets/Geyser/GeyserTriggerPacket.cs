using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Geyser;

public sealed class GeyserTriggerPacket : IPacket
{
    // Couldnt find an ID system for these so I need to access them through GameObject.Find
    public string ObjectPath { get; set; }

    public PacketType Type => PacketType.GeyserTrigger;

    public void Serialise(PacketWriter writer) => writer.WriteString(ObjectPath);

    public void Deserialise(PacketReader reader) => ObjectPath = reader.ReadString();
}