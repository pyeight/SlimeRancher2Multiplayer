using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct WorldFXPacket : IPacket
{
    public enum WorldFXType : byte
    {
        None,
        BuyPlot,
        UpgradePlot,
        SellPlort,
        SellPlortSound,
        SellPlortDroneSound,
    }

    public byte Type { get; set; }
    public WorldFXType FX { get; set; }

    public Vector3 Position { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteEnum(FX);
        writer.WriteVector3(Position);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        FX = reader.ReadEnum<WorldFXType>();
        Position = reader.ReadVector3();
    }
}