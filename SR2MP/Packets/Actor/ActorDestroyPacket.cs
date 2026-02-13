using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

public struct ActorDestroyPacket : IPacket
{
    public ActorId ActorId;

    public readonly PacketType Type => PacketType.ActorDestroy;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer) => writer.WriteLong(ActorId.Value);

    public void Deserialise(PacketReader reader) => ActorId = new ActorId(reader.ReadLong());
}