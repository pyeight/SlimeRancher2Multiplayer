using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;
public struct LightningStrikePacket : IPacket
{
    public PacketType Type => PacketType.LightningStrike;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public Vector3 Position { get; set; }

    public readonly void Serialise(PacketWriter writer) 
    {
         writer.WriteVector3(Position);
    }

    public void Deserialise(PacketReader reader)     
    { 
        Position = reader.ReadVector3();
    }
}