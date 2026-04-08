using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerGadgetUpdatePacket : IPacket
{
    public bool Enabled;
    public string PlayerId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Quaternion GadgetLocalRotation;
    public int CurrentGadget;
    public bool ValidPlacement;

    public PacketType Type => PacketType.PlayerGadgetUpdate;
    public PacketReliability Reliability => PacketReliability.UnreliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteBool(Enabled);

        writer.WriteString(PlayerId);

        if (!Enabled) return;
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteQuaternion(GadgetLocalRotation);
        writer.WriteInt(CurrentGadget);
        writer.WriteBool(ValidPlacement);
    }

    public void Deserialise(PacketReader reader)
    {
        Enabled = reader.ReadBool();
        PlayerId = reader.ReadStringWithSize(16)!;

        if (!Enabled) return;

        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        GadgetLocalRotation = reader.ReadQuaternion();
        CurrentGadget = reader.ReadInt();
        ValidPlacement = reader.ReadBool();
    }
}