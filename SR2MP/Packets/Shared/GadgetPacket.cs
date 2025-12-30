using UnityEngine;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct GadgetPacket : IPacket
{
    public byte Type { get; set; }
    public string GadgetId { get; set; }
    public int GadgetTypeId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public bool IsRemoval { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(GadgetId);
        writer.WriteInt(GadgetTypeId);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
        writer.WriteBool(IsRemoval);
    }

    public void Deserialise(PacketReader reader)
    {
        GadgetId = reader.ReadString();
        GadgetTypeId = reader.ReadInt();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
        IsRemoval = reader.ReadBool();
    }
}
