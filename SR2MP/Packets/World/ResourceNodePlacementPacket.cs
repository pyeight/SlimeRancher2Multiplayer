using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class ResourceNodePlacement
{
    public string ID = string.Empty;
    public bool HasNode;
    public byte State;
    public int DefinitionIndex = -1;
    public int VariantIndex = -1;
    public double DespawnAtWorldTime;
    public readonly List<int> ResourcesToSpawn = new();

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(HasNode);

        if (!HasNode)
            return;

        writer.WriteByte(State);
        writer.WriteInt(DefinitionIndex);
        writer.WriteInt(VariantIndex);
        writer.WriteDouble(DespawnAtWorldTime);

        writer.WriteInt(ResourcesToSpawn.Count);
        foreach (var typeId in ResourcesToSpawn)
            writer.WriteInt(typeId);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        HasNode = reader.ReadBool();

        if (!HasNode)
            return;

        State = reader.ReadByte();
        DefinitionIndex = reader.ReadInt();
        VariantIndex = reader.ReadInt();
        DespawnAtWorldTime = reader.ReadDouble();

        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
            ResourcesToSpawn.Add(reader.ReadInt());
    }
}

internal sealed class ResourceNodePlacementPacket : IPacket
{
    public ResourceNodePlacement Node = new();

    public PacketType Type => PacketType.ResourceNodePlacement;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => Node.Serialise(writer);

    public void Deserialise(PacketReader reader) => Node.Deserialise(reader);
}
