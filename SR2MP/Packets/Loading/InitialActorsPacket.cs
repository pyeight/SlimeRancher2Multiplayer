using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal partial class InitialActorsPacket : IPacket
{
    private static readonly ReadDel<ActorBase> ReadFunction = reader =>
    {
        var actorTypeEnum = reader.ReadEnum<ActorType>();
        var actorType = actorTypes![actorTypeEnum];
        var actor = (ActorBase)Activator.CreateInstance(actorType)!;

        actor.Deserialise(reader);

        SrLogger.LogDebug($"{actorTypeEnum} Actor: {actor.ActorId}");

        return actor;
    };

    public double WorldTime;
    public uint StartingActorID;
    public List<ActorBase> Actors;

    public PacketType Type => PacketType.InitialActors;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.ActorCritical;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedUInt(StartingActorID);
        writer.WriteDouble(WorldTime);
        writer.WriteList(Actors, PacketWriterDels.NetObject<ActorBase>.Writer);
    }

    public void Deserialise(PacketReader reader)
    {
        StartingActorID = reader.ReadPackedUInt();
        WorldTime = reader.ReadDouble();
        Actors = reader.ReadList(ReadFunction)!;
    }
}