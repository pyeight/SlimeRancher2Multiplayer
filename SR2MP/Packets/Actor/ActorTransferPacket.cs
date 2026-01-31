using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class ActorTransferPacket : PacketBase
{
    public ActorId ActorId { get; set; }
    public string OwnerPlayer { get; set; }

    public override PacketType Type => PacketType.ActorTransfer;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteString(OwnerPlayer);
    }

    public override void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        OwnerPlayer = reader.ReadString();
    }
}