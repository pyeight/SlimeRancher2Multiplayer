using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public sealed class UpgradesPacket : IPacket
{
    public byte Type { get; set; }

    // I really dislike the idea, but I'd rather send a List of integers to the Client than a (shorter) list of strings of string ~ Artur
    public Dictionary<byte, sbyte> Upgrades { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteDictionary(Upgrades, (writer2, value) => writer2.WriteByte(value), (writer2, value) => writer2.WriteSByte(value));
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Upgrades = reader.ReadDictionary(reader2 => reader2.ReadByte(), reader2 => reader2.ReadSByte());
    }
}