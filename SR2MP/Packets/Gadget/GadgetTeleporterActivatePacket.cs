using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Gadget;

internal sealed class GadgetTeleporterActivatePacket : IPacket
{
    public string ObjectPath;
    public bool IsArrival;

    public PacketType Type => PacketType.GadgetTeleporterFX;
    public PacketReliability Reliability => PacketReliability.Unreliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ObjectPath);
        writer.WriteBool(IsArrival);
    }

    public void Deserialise(PacketReader reader)
    {
        ObjectPath = reader.ReadPooledString()!;
        IsArrival = reader.ReadBool();
    }
}
