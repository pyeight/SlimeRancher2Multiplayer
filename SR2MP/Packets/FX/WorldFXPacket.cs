using SR2MP.Packets.Utils;

namespace SR2MP.Packets.FX;

internal struct WorldFXPacket : IPacket
{
    internal enum WorldFXType : byte
    {
        None,
        BuyPlot,
        UpgradePlot,
        SellPlort,
        SellPlortSound,
        SellPlortDroneSound,
        FavoriteFoodEaten, // Also applies to gordo slimes.
        GordoFoodEaten,
        GordoFoodEatenSound
        // FabricatorPurchaseGadget,
        // FabricatorPurchaseUpgrade,
    }

    public Vector3 Position;

    public WorldFXType FX;

    public readonly PacketType Type => PacketType.WorldFX;
    public readonly PacketReliability Reliability => PacketReliability.Unreliable;
    public readonly NetworkChannel Channel => NetworkChannel.FX;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteEnum(FX);
        writer.WriteVector3(Position);
    }

    public void Deserialise(PacketReader reader)
    {
        FX = reader.ReadEnum<WorldFXType>();
        Position = reader.ReadVector3();
    }
}