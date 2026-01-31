using SR2MP.Packets.Utils;

namespace SR2MP.Packets.FX;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.Unreliable, channel: SR2MP.Networking.NetChannels.Fx)]
public sealed class WorldFXPacket : PacketBase
{
    public enum WorldFXType : byte
    {
        None,
        BuyPlot,
        UpgradePlot,
        SellPlort,
        SellPlortSound,
        SellPlortDroneSound,
        FavoriteFoodEaten, // Also applies to gordos.
        GordoFoodEaten,
        GordoFoodEatenSound,
    }

    public Vector3 Position { get; set; }

    public WorldFXType FX { get; set; }

    public override PacketType Type => PacketType.WorldFX;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteEnum(FX);
        writer.WriteVector3(Position);
    }

    public override void Deserialise(PacketReader reader)
    {
        FX = reader.ReadEnum<WorldFXType>();
        Position = reader.ReadVector3();
    }
}