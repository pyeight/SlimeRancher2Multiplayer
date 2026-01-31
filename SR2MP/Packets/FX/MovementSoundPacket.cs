using SR2MP.Packets.Utils;

namespace SR2MP.Packets.FX;

// Do not rewrite this, the movement SFX comes in many materials and types (36 different sounds).
// Il2CppMonomiPark.SlimeRancher.VFX.EnvironmentInteraction;
// GroundCollisionMaterials.GroundCollisionMaterialType.X
[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.Unreliable, channel: SR2MP.Networking.NetChannels.Fx)]
public sealed class MovementSoundPacket : PacketBase
{
    public Vector3 Position { get; set; }
    public string CueName { get; set; }

    public override PacketType Type => PacketType.MovementSound;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteVector3(Position);
        writer.WriteString(CueName);
    }

    public override void Deserialise(PacketReader reader)
    {
        Position = reader.ReadVector3();
        CueName = reader.ReadString();
    }
}