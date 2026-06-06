using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

internal sealed class InitialLandPlotsPacket : IPacket
{
    internal sealed class BasePlot : INetObject
    {
        public override string ToString() => ID;

        private static readonly Dictionary<LandPlot.Id, Type> DataTypes = new()
        {
            { LandPlot.Id.GARDEN,      typeof(GardenData)      },
            { LandPlot.Id.SILO,        typeof(SiloData)        },
            { LandPlot.Id.CORRAL,      typeof(CorralData)      },
            { LandPlot.Id.COOP,        typeof(CoopPondData)    },
            { LandPlot.Id.POND,        typeof(CoopPondData)    },
            { LandPlot.Id.INCINERATOR, typeof(IncineratorData) },
        };

        public string ID;
        public LandPlot.Id  Type;
        public CppCollections.HashSet<LandPlot.Upgrade> Upgrades;

        public INetObject? Data;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WritePackedEnum(Type);
            writer.WriteCppHashSet(Upgrades, PacketWriterDels.PackedEnum<LandPlot.Upgrade>.Writer);

            Data?.Serialise(writer);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadPooledString()!;
            Type = reader.ReadPackedEnum<LandPlot.Id>();
            Upgrades = reader.ReadCppHashSet(PacketReaderDels.PackedEnum<LandPlot.Upgrade>.Reader)!;

            if (!DataTypes.TryGetValue(Type, out var dataType))
            {
                SrLogger.LogDebug($"{ID} -> (No Data)");
                return;
            }

            SrLogger.LogDebug($"{ID} -> {dataType.Name}");
            Data = (INetObject)Activator.CreateInstance(dataType)!;
            Data.Deserialise(reader);
        }
    }

    internal struct GardenData : INetObject
    {
        public int Crop;

        public readonly void Serialise(PacketWriter writer) => writer.WriteInt(Crop);

        public void Deserialise(PacketReader reader) => Crop = reader.ReadInt();
    }

    internal sealed class SiloData : INetObject
    {
        public List<byte> SelectedSlots;
        public NetworkAmmo Ammo;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteNetObject(Ammo);
            writer.WriteList(SelectedSlots, PacketWriterDels.Byte);
        }

        public void Deserialise(PacketReader reader)
        {
            Ammo = reader.ReadNetObject<NetworkAmmo>();
            SelectedSlots = reader.ReadList(PacketReaderDels.Byte)!;
        }
    }

    internal sealed class CorralData : INetObject
    {
        public NetworkAmmo PlortCollectorAmmo;
        public NetworkAmmo AutoFeederAmmo;
        public byte AutoFeederSpeed;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteNetObject(PlortCollectorAmmo);
            writer.WriteNetObject(AutoFeederAmmo);
            writer.WriteByte(AutoFeederSpeed);
        }

        public void Deserialise(PacketReader reader)
        {
            PlortCollectorAmmo = reader.ReadNetObject<NetworkAmmo>();
            AutoFeederAmmo = reader.ReadNetObject<NetworkAmmo>();
            AutoFeederSpeed = reader.ReadByte();
        }
    }

    // Data for Coop or Pond
    internal sealed class CoopPondData : INetObject
    {
        public NetworkAmmo? CollectorAmmo;

        public  void Serialise(PacketWriter writer)
        {
            writer.WriteBool(CollectorAmmo != null);
            if (CollectorAmmo != null)
                writer.WriteNetObject(CollectorAmmo);
        }

        public void Deserialise(PacketReader reader)
        {
            if (reader.ReadBool())
                CollectorAmmo = reader.ReadNetObject<NetworkAmmo>();
        }
    }

    // Data for Incinerators
    internal sealed class IncineratorData : INetObject
    {
        public NetworkAmmo? PlortCollectorAmmo;
        public float AshLevel;

        public void Serialise(PacketWriter writer)
        {
            writer.WriteBool(PlortCollectorAmmo != null);
            if (PlortCollectorAmmo != null)
                writer.WriteNetObject(PlortCollectorAmmo);
            
            writer.WriteFloat(AshLevel);
        }

        public void Deserialise(PacketReader reader)
        {
            if (reader.ReadBool())
                PlortCollectorAmmo = reader.ReadNetObject<NetworkAmmo>();
            
            AshLevel = reader.ReadFloat();
        }
    }

    public List<BasePlot> LandPlots;

    public PacketType Type => PacketType.InitialLandPlots;
    public PacketReliability Reliability => PacketReliability.Reliable;
    public NetworkChannel Channel => NetworkChannel.Landplots;

    public void Serialise(PacketWriter writer) => writer.WriteList(LandPlots, PacketWriterDels.NetObject<BasePlot>.Writer);

    public void Deserialise(PacketReader reader) => LandPlots = reader.ReadList(PacketReaderDels.NetObject<BasePlot>.Reader)!;
}