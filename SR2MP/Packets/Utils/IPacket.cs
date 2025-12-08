using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Utils;

public interface IPacket
{
    public void Serialise(PacketWriter writer);

    public void Deserialise(PacketReader reader);
}