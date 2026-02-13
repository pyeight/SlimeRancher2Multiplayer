using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

// Not sure what to call it, 'unload' as in when the actor leaves render distance.
public struct ActorUnloadPacket : IPacket
{
    public ActorId ActorId;

    public readonly PacketType Type => PacketType.ActorUnload;
    public readonly PacketReliability Reliability => PacketReliability.Reliable;

    public readonly void Serialise(PacketWriter writer) => writer.WriteLong(ActorId.Value);

    public void Deserialise(PacketReader reader) => ActorId = new ActorId(reader.ReadLong());
}