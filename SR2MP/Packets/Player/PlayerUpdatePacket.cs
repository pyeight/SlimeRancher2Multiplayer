using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerUpdatePacket : IPacket
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public int AirborneState {get; set;}
    public bool Moving { get; set; }
    public float Yaw { get; set; }
    public float HorizontalMovement { get; set; }
    public float ForwardMovement { get; set; }
    public float HorizontalSpeed { get; set; }
    public float ForwardSpeed { get; set; }
    public bool Sprinting { get; set; }
    public float LookY { get; set; }

    public PacketType Type => PacketType.PlayerUpdate;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);

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
        writer.EndPackingBools();
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();

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
        reader.EndPackingBools();
    }
}