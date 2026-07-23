using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class TornadoSpawnPacket : IPacket
{
    public int TornadoId;
    public bool Despawn;
    public Vector3 Position;
    public Quaternion Rotation;

    public PacketType Type => PacketType.TornadoSpawn;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Weather;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(TornadoId);
        writer.WriteBool(Despawn);
        if (!Despawn)
        {
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        TornadoId = reader.ReadPackedInt();
        Despawn = reader.ReadBool();
        if (!Despawn)
        {
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
        }
    }
}
