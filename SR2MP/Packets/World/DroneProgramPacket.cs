using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class DroneProgramPacket : IPacket
{
    public long ActorId;
    public int TargetIdent;
    public byte Target;
    public byte Sink;
    public byte Source;

    public PacketType Type => PacketType.DroneProgram;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId);
        writer.WritePackedInt(TargetIdent);
        writer.WriteByte(Target);
        writer.WriteByte(Sink);
        writer.WriteByte(Source);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = reader.ReadPackedLong();
        TargetIdent = reader.ReadPackedInt();
        Target = reader.ReadByte();
        Sink = reader.ReadByte();
        Source = reader.ReadByte();
    }
}
