using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

// Do not rewrite this, the movement SFX comes in many materials and types (36 different sounds).
// Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction;
// GroundCollisionMaterials.GroundCollisionMaterialType.X
public struct MovementSoundPacket : IPacket
{
    public byte Type { get; set; }
    public Vector3 Position { get; set; }
    public string CueName { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteVector3(Position);
        writer.WriteString(CueName);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Position = reader.ReadVector3();
        CueName = reader.ReadString();
    }
}