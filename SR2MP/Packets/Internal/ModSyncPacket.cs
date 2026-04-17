using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Internal;

internal sealed class ModSyncPacket : IPacket
{
    public string PlayerId;
    public Dictionary<uint, ModData> Mods;

    public PacketType Type => PacketType.ModSyncAck;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Important;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);
        writer.WriteDictionary(Mods, PacketWriterDels.UInt, PacketWriterDels.NetObject<ModData>.Writer);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadPooledStringOfSize(16)!;
        Mods = reader.ReadDictionary(PacketReaderDels.UInt, PacketReaderDels.NetObject<ModData>.Reader)!;
    }
}

internal sealed class ModData : INetObject
{
    public string Name;
    public string Version;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteString(Version);
    }

    public void Deserialise(PacketReader reader)
    {
        Name = reader.ReadPooledString()!;
        Version = reader.ReadPooledString()!;
    }
}