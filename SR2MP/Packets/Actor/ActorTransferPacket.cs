using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

public sealed class ActorTransferPacket : IPacket
{
    public ActorId ActorId;
    public string OwnerId;

    public PacketType Type => PacketType.ActorTransfer;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedLong(ActorId.Value);
        writer.WriteStringWithoutSize(OwnerId);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        OwnerId = reader.ReadStringWithSize(15)!;
    }
}