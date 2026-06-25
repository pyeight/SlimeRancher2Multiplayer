using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

// Not sure what to call it, 'unload' as in when the actor leaves render distance.
internal sealed class ActorUnloadPacket : IPacket
{
    public ActorId ActorId;
    public string SenderId = string.Empty;

    public PacketType Type => PacketType.ActorUnload;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.ActorCritical;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WriteString(SenderId);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        SenderId = reader.ReadPooledString() ?? string.Empty;
    }
}