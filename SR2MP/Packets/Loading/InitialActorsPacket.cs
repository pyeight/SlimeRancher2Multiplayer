using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed partial class InitialActorsPacket : IPacket
{
    private static Func<PacketReader, ActorBase> readFunction = reader =>
    {
        var actorTypeEnum = reader.ReadEnum<ActorType>();

        // Fuck ass compiler throwing a warning here for some reason
#pragma warning disable CS8602 // Dereference of a possibly null reference
        var actorType = actorTypes[actorTypeEnum];
#pragma warning restore CS8602 // Dereference of a possibly null reference

        var actor = (ActorBase)Activator.CreateInstance(actorType)!;

        actor.Deserialise(reader);

        SrLogger.LogMessage($"{actorTypeEnum} Actor: {actor.ActorId}");

        return actor;
    };

    public uint StartingActorID { get; set; } = 10000;
    public List<ActorBase> Actors { get; set; }

    public PacketType Type => PacketType.InitialActors;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUInt(StartingActorID);
        writer.WriteList(Actors, PacketWriterDels.NetObject<ActorBase>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        StartingActorID = reader.ReadUInt();
        Actors = reader.ReadList(readFunction);
    }
}