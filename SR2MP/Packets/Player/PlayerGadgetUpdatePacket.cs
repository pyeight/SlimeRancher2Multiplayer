using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

internal sealed class PlayerGadgetUpdatePacket : IPacket
{
    public bool Enabled;
    public string PlayerId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Quaternion GadgetLocalRotation;
    public int CurrentGadget;
    public bool ValidPlacement;

    public PacketType Type => PacketType.PlayerGadgetUpdate;
    public PacketReliability Reliability => PacketReliability.Ordered;
    public NetworkChannel Channel => NetworkChannel.PlayerUpdate;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedBool(Enabled);
        writer.WriteString(PlayerId);

        if (!Enabled) return;
        
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteQuaternion(GadgetLocalRotation);
        writer.WritePackedInt(CurrentGadget);
        writer.WritePackedBool(ValidPlacement);
    }

    public void Deserialise(PacketReader reader)
    {
        Enabled = reader.ReadPackedBool();
        PlayerId = reader.ReadString()!;
        
        if (!Enabled) return;
        
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        GadgetLocalRotation = reader.ReadQuaternion();
        CurrentGadget = reader.ReadPackedInt();
        ValidPlacement = reader.ReadPackedBool();
    }
}