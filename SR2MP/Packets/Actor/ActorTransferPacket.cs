using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Actor;

public sealed class ActorTransferPacket : IPacket
{
    public ActorId ActorId { get; set; }
    public string OwnerPlayer { get; set; }

    public PacketType Type => PacketType.ActorTransfer;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteLong(ActorId.Value);
        writer.WriteString(OwnerPlayer);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadLong());
        OwnerPlayer = reader.ReadString();
    }
}