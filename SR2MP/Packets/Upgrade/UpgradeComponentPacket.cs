// todo
// what is this even supposed to be?
// It's implemented now!

using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Upgrade;

internal sealed class UpgradeComponentPacket : IPacket
{
    public string ComponentId;
    public byte Count;

    public PacketType Type => PacketType.ComponentAdd;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ComponentId);
        writer.WriteByte(Count);
    }

    public void Deserialise(PacketReader reader)
    {
        ComponentId = reader.ReadPooledString()!;
        Count = reader.ReadByte();
    }
}
