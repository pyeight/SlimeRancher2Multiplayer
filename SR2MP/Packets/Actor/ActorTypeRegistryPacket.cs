using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

internal struct ActorTypeRegistryPacket : IPacket
{
    public Dictionary<int, string> Registry;

    public readonly PacketType Type => PacketType.ActorTypeRegistry;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;
    public readonly NetworkChannel Channel => NetworkChannel.ActorCritical;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Registry.Count);
        foreach (var (persistentId, referenceId) in Registry)
        {
            writer.WritePackedInt(persistentId);
            writer.WriteString(referenceId);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        var count = reader.ReadPackedInt();
        Registry = new Dictionary<int, string>(count);
        for (var i = 0; i < count; i++)
        {
            var persistentId = reader.ReadPackedInt();
            var referenceId  = reader.ReadString();
            Registry[persistentId] = referenceId!;
        }
    }
}