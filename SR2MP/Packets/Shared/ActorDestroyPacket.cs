using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct ActorDestroyPacket : IPacket
{
    public byte Type { get; set; }
    
    public ActorId ActorId { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteLong(ActorId.Value);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        ActorId = new ActorId(reader.ReadLong());
    }
}