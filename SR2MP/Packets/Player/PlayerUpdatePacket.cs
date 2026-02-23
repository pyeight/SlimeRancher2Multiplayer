using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerUpdatePacket : IPacket
{
    public string PlayerId;
    public Vector3 Position;
    public float Rotation;
    public int AirborneState;
    public bool Moving;
    public float Yaw;
    public float HorizontalMovement;
    public float ForwardMovement;
    public float HorizontalSpeed;
    public float ForwardSpeed;
    public bool Sprinting;
    public float LookY;

    public PacketType Type => PacketType.PlayerUpdate;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteStringWithoutSize(PlayerId);

        writer.WriteVector3(Position);
        writer.WriteFloat(Rotation);

        writer.WriteInt(AirborneState);
        writer.WriteFloat(Yaw);
        writer.WriteFloat(HorizontalMovement);
        writer.WriteFloat(ForwardMovement);
        writer.WriteFloat(HorizontalSpeed);
        writer.WriteFloat(ForwardSpeed);

        writer.WriteFloat(LookY);

        writer.WritePackedBool(Moving);
        writer.WritePackedBool(Sprinting);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadStringWithSize(15)!;

        Position = reader.ReadVector3();
        Rotation = reader.ReadFloat();

        AirborneState = reader.ReadInt();
        Yaw = reader.ReadFloat();
        HorizontalMovement = reader.ReadFloat();
        ForwardMovement = reader.ReadFloat();
        HorizontalSpeed = reader.ReadFloat();
        ForwardSpeed = reader.ReadFloat();

        LookY = reader.ReadFloat();

        Moving = reader.ReadPackedBool();
        Sprinting = reader.ReadPackedBool();
    }
}