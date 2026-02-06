using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed partial class InitialActorsPacket : IPacket
{
    private static Func<PacketReader, ActorBase> readFunction = reader =>
    {
        var actorTypeEnum = reader.ReadEnum<ActorType>();
        var actorType = actorTypes![actorTypeEnum];
        var actor = (ActorBase)Activator.CreateInstance(actorType)!;

        actor.Deserialise(reader);

        SrLogger.LogMessage($"{actorTypeEnum} Actor: {actor.ActorId}");

        return actor;
    };

    public double WorldTime { get; set; }
    public uint StartingActorID { get; set; } = 10000;
    public List<ActorBase> Actors { get; set; }

    public PacketType Type => PacketType.InitialActors;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteUInt(StartingActorID);
        writer.WriteDouble(WorldTime);
        writer.WriteList(Actors, PacketWriterDels.NetObject<ActorBase>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        StartingActorID = reader.ReadUInt();
        WorldTime = reader.ReadDouble();
        Actors = reader.ReadList(readFunction);
    }
}