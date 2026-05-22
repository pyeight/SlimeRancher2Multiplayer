using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Slime.Sloomber;

internal sealed class SloomberYawnPacket : IPacket
{
    public ActorId ActorId;
    public bool Active;
    
    public PacketType Type => PacketType.SloomberYawn;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.ActorCritical;

    public void Serialise(PacketWriter writer)
    { 
        writer.WritePackedLong(ActorId.Value);
        writer.WriteBool(Active);
    }

    public void Deserialise(PacketReader reader)
    {
        ActorId = new ActorId(reader.ReadPackedLong());
        Active = reader.ReadBool();
    }
}