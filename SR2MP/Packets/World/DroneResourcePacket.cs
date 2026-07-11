using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class DroneResourcePacket : IPacket
{
    internal sealed class DroneResourceEntry : INetObject
    {
        public bool Valid;
        public string Id = string.Empty;
        public Vector3 Location;
        public Vector3 OffsetLocation;
        public List<int> TypeIds = new();

        public void Serialise(PacketWriter writer)
        {
            writer.WriteBool(Valid);

            if (!Valid)
                return;

            writer.WriteString(Id);
            writer.WriteVector3(Location);
            writer.WriteVector3(OffsetLocation);
            writer.WritePackedInt(TypeIds.Count);

            foreach (var typeId in TypeIds)
                writer.WritePackedInt(typeId);
        }

        public void Deserialise(PacketReader reader)
        {
            Valid = reader.ReadBool();

            if (!Valid)
                return;

            Id = reader.ReadPooledString()!;
            Location = reader.ReadVector3();
            OffsetLocation = reader.ReadVector3();

            var count = reader.ReadPackedInt();
            TypeIds = new List<int>(count);

            for (var i = 0; i < count; i++)
                TypeIds.Add(reader.ReadPackedInt());
        }
    }
    
    public int Scene;
    public int Index;
    public DroneResourceEntry Entry = new();

    public PacketType Type => PacketType.DroneResourceUpdate;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Scene);
        writer.WritePackedInt(Index);
        // Entry = new DroneResourceEntry();
        Entry.Serialise(writer);
    }

    public void Deserialise(PacketReader reader)
    {
        Scene = reader.ReadPackedInt();
        Index = reader.ReadPackedInt();
        // Entry = new DroneResourceEntry();
        Entry.Deserialise(reader);
    }
}
