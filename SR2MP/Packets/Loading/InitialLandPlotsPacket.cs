using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class InitialLandPlotsPacket : IPacket
{
    public sealed class BasePlot : INetObject
    {
        public static Dictionary<LandPlot.Id, Type> dataTypes = new()
        {
            { LandPlot.Id.GARDEN, typeof(GardenData) },
            { LandPlot.Id.SILO,   typeof(SiloData)   }
        };

        public string ID { get; set; }
        public LandPlot.Id Type { get; set; }
        public CppCollections.HashSet<LandPlot.Upgrade> Upgrades { get; set; }

        public INetObject? Data { get; set; }
        
        public void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(Type);
            writer.WriteCppSet(Upgrades, PacketWriterDels.Enum<LandPlot.Upgrade>.Func);
            
            Data?.Serialise(writer);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            Type = reader.ReadEnum<LandPlot.Id>();
            Upgrades = reader.ReadCppSet(PacketReaderDels.Enum<LandPlot.Upgrade>.Func);

            if (dataTypes.TryGetValue(Type, out var dataType))
            {
                Data = (INetObject)Activator.CreateInstance(dataType)!;

                Data.Deserialise(reader);
            }
        }
    }

    public struct GardenData : INetObject
    {
        public int Crop { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteInt(Crop);
        }

        public void Deserialise(PacketReader reader)
        {
            Crop = reader.ReadInt();
        }
    }
    public struct SiloData : INetObject
    {
        // todo
        public readonly void Serialise(PacketWriter writer) { }
        public void Deserialise(PacketReader reader) { }
    }

    public List<BasePlot> Plots { get; set; }

    public PacketType Type => PacketType.InitialPlots;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer) => writer.WriteList(Plots, PacketWriterDels.NetObject<BasePlot>.Func);

    public void Deserialise(PacketReader reader) => Plots = reader.ReadList(PacketReaderDels.NetObject<BasePlot>.Func);
}