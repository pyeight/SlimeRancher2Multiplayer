using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialPuzzleSlotsPacket : IPacket
{
    internal sealed class Slot : INetObject
    {
        public string ID;
        public bool Filled;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteBool(Filled);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadPooledString()!;
            Filled = reader.ReadBool();
        }
    }

    public List<Slot> Slots;

    public PacketType Type => PacketType.InitialPuzzleSlots;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer) => writer.WriteList(Slots, PacketWriterDels.NetObject<Slot>.Writer);

    public void Deserialise(PacketReader reader) => Slots = reader.ReadList(PacketReaderDels.NetObject<Slot>.Reader)!;
}
