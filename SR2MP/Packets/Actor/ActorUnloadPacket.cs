using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

// Not sure what to call it, 'unload' as in when the actor leaves render distance.
[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class ActorUnloadPacket : PacketBase
{
    public ActorId ActorId { get; set; }

    public override PacketType Type => PacketType.ActorUnload;

    public override void Serialise(PacketWriter writer) => writer.WriteLong(ActorId.Value);

    public override void Deserialise(PacketReader reader) => ActorId = new ActorId(reader.ReadLong());
}