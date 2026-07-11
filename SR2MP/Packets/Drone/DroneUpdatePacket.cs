using SR2MP.Components.Drone;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Drone;

internal sealed class DroneUpdatePacket : IPacket
{
    public long StationId;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool AtStation;
    public int AnimatorState;
    public List<NetworkDrone.AnimatorParameter> AnimatorParams = new();
    public NetworkAmmo? Ammo;

    public PacketType Type => PacketType.DroneUpdate;
    public PacketReliability Reliability => PacketReliability.Ordered;
    public NetworkChannel Channel => NetworkChannel.ActorUpdate;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(StationId);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteBool(AtStation);
        writer.WriteInt(AnimatorState);

        writer.WriteByte((byte)AnimatorParams.Count);
        foreach (var param in AnimatorParams)
        {
            writer.WriteInt(param.Hash);
            writer.WriteByte(param.Type);
            writer.WriteFloat(param.Value);
        }

        writer.WriteBool(Ammo != null);
        if (Ammo != null)
            writer.WriteNetObject(Ammo);
    }

    public void Deserialise(PacketReader reader)
    {
        StationId = reader.ReadPackedLong();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        AtStation = reader.ReadBool();
        AnimatorState = reader.ReadInt();

        var paramCount = reader.ReadByte();
        AnimatorParams = new List<NetworkDrone.AnimatorParameter>(paramCount);
        for (var i = 0; i < paramCount; i++)
        {
            AnimatorParams.Add(new NetworkDrone.AnimatorParameter
            {
                Hash = reader.ReadInt(),
                Type = reader.ReadByte(),
                Value = reader.ReadFloat()
            });
        }

        if (reader.ReadBool())
            Ammo = reader.ReadNetObject<NetworkAmmo>();
    }
}