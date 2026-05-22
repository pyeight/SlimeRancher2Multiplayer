using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;
using UnityEngine;

namespace SR2MP.Packets.Slime.Dervish;

internal sealed class DervishCyclonePacket : IPacket
{
    public ActorId ActorId;
    public bool Active;
    public byte Size;
    public Vector3 FloatDir;

    public PacketType Type => PacketType.DervishCyclone;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.ActorCritical;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WritePackedBool(Active);
        writer.WriteByte(Size);
        writer.WriteVector3(FloatDir);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        Active = reader.ReadPackedBool();
        Size = reader.ReadByte();
        FloatDir = reader.ReadVector3();
    }
}