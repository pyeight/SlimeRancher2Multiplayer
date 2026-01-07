namespace SR2MP.Packets.Utils;

// All packet types MUST have a parameter-less constructor! Either make the type a struct (which always has a parameterless constructor), or at least declare a parameterless constructor for classes!
public interface IPacket
{
    void Serialise(PacketWriter writer);

    void Deserialise(PacketReader reader);
}