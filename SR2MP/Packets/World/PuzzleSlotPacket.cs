using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class PuzzleSlotPacket : IPacket
{
    public string ID;
    public bool Filled;

    public PacketType Type => PacketType.PuzzleSlot;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(Filled);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        Filled = reader.ReadBool();
    }
}
