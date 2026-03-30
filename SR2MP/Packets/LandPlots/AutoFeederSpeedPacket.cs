using SR2MP.Packets.Utils;

namespace SR2MP.Packets.LandPlots;

internal sealed class AutoFeederSpeedPacket : IPacket
{
    public SlimeFeeder.FeedSpeed Speed;
    public string ID;

    public PacketType Type => PacketType.AutoFeederSpeed;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedEnum(Speed);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString()!;
        Speed = reader.ReadPackedEnum<SlimeFeeder.FeedSpeed>();
    }
}