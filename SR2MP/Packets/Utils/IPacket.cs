namespace SR2MP.Packets.Utils;

public interface IPacket
{
    void Serialise(PacketWriter writer);

    void Deserialise(PacketReader reader);
}