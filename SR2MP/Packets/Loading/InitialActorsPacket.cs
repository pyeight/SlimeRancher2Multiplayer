using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed partial class InitialActorsPacket : IPacket
{
    private static Func<PacketReader, ActorBase> readFunction = reader =>
    {
        var actorTypeEnum = reader.ReadEnum<ActorType>();

        var actorType = actorTypes[actorTypeEnum];
        var actor = (ActorBase)Activator.CreateInstance(actorType)!;

        actor.Deserialise(reader);

        SrLogger.LogMessage($"{actorTypeEnum} Actor: {actor.ActorId}");
        
        return actor;
    };

    private static Action<PacketWriter, ActorBase> writeFunction = (writer, actor) =>
    {
        ActorType actorType = ActorType.Basic;
        if (actor is Slime)
            actorType = ActorType.Slime;
        else if (actor is Plort)
            actorType = ActorType.Plort;
        else if (actor is Resource)
            actorType = ActorType.Resource;
        writer.WriteEnum(actorType);

        actor.Serialise(writer);
    };
    
    public uint StartingActorID { get; set; } = 10000;
    public List<ActorBase> Actors { get; set; }

    public PacketType Type => PacketType.InitialActors;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUInt(StartingActorID);
        writer.WriteList(Actors, writeFunction);
    }

    public void Deserialise(PacketReader reader)
    {
        StartingActorID = reader.ReadUInt();
        Actors = reader.ReadList(readFunction);
    }
}