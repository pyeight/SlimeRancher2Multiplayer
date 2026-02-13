using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

public struct LightningStrikePacket : IPacket
{
    public Vector3 Position;

    public readonly PacketType Type => PacketType.LightningStrike;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;

    public readonly void Serialise(PacketWriter writer) => writer.WriteVector3(Position);

    public void Deserialise(PacketReader reader) => Position = reader.ReadVector3();
}