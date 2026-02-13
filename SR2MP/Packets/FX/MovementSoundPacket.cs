using SR2MP.Packets.Utils;

namespace SR2MP.Packets.FX;

// Do not rewrite this, the movement SFX comes in many materials and types (36 different sounds).
// Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction;
// GroundCollisionMaterials.GroundCollisionMaterialType.X
public sealed class MovementSoundPacket : IPacket
{
    public Vector3 Position;
    public string CueName;

    public PacketType Type => PacketType.MovementSound;
    public PacketReliability Reliability => PacketReliability.Unreliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteString(CueName);
    }

    public void Deserialise(PacketReader reader)
    {
        Position = reader.ReadVector3();
        CueName = reader.ReadString();
    }
}