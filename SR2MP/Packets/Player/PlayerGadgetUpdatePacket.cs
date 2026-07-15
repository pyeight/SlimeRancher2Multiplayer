using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

/// <summary>
/// All gadget placement validity states
/// </summary>
public enum GadgetPlacementValidity : byte
{
    /// <summary>The gadget cannot be placed here.</summary>
    Invalid,

    /// <summary>PlacementImprovements mod: overrides the game's rejection (clipping).</summary>
    AlmostValid,

    /// <summary>The gadget can be placed here.</summary>
    Valid,
}

internal sealed class PlayerGadgetUpdatePacket : IPacket
{
    public bool Enabled;
    public string PlayerId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Quaternion GadgetLocalRotation;
    public int CurrentGadget;
    public GadgetPlacementValidity Validity;

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
        writer.WriteByte((byte)Validity);
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
        Validity = (GadgetPlacementValidity)reader.ReadByte();
    }
}