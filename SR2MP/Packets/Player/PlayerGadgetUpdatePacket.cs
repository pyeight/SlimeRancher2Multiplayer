using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Player;

public sealed class PlayerGadgetUpdatePacket : IPacket
{
    public bool Enabled;

    public string PlayerId;
    public Vector3 Position;
    public Vector3 Rotation;
    public int CurrentGadget = -2;

    public PacketType Type => PacketType.PlayerGadgetUpdate;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteBool(Enabled);
        writer.WriteStringWithoutSize(PlayerId);
        if (!Enabled) return;
        writer.WriteVector3(Position);
        writer.WriteVector3(Rotation);
        writer.WriteInt(CurrentGadget);
    }

    public void Deserialise(PacketReader reader)
    {
        Enabled = reader.ReadBool();
        PlayerId = reader.ReadStringWithSize(15)!;
        if (!Enabled) return;
        Position = reader.ReadVector3();
        Rotation = reader.ReadVector3();
        CurrentGadget = reader.ReadInt();
    }
}