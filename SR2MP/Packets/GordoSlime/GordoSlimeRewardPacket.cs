using SR2MP.Packets.Utils;

namespace SR2MP.Packets.GordoSlime;

internal sealed class GordoSlimeRewardPacket : IPacket
{
    public string GordoSlimeId;
    public int PrefabIndex;
    public Vector3 Position;
    public Quaternion Rotation;

    public PacketType Type => PacketType.GordoSlimeReward;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(GordoSlimeId);
        writer.WritePackedInt(PrefabIndex);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
    }

    public void Deserialise(PacketReader reader)
    {
        GordoSlimeId = reader.ReadPooledString()!;
        PrefabIndex = reader.ReadPackedInt();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}
