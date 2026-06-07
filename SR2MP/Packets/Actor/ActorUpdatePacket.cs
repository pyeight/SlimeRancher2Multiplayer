using System.Runtime.InteropServices;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

[StructLayout(LayoutKind.Auto)]
internal struct ActorUpdatePacket : IPacket
{
    public ActorId ActorId;

    public Quaternion Rotation;
    public Vector3 Position;
    public Vector3 Velocity;

    public readonly PacketType Type => PacketType.ActorUpdate;
    public readonly PacketReliability Reliability => PacketReliability.Ordered;
    public readonly NetworkChannel Channel => NetworkChannel.ActorUpdate;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteVector3(Velocity);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId  = new ActorId(reader.ReadLong());
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        Velocity = reader.ReadVector3();
    }
}