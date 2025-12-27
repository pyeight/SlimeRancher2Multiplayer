using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C;

public sealed class LandPlotsPacket : IPacket
{
    public struct Plot : IPacket
    {
        public string ID { get; set; }
        public LandPlot.Id Type { get; set; }

        internal Il2CppSystem.Collections.Generic.List<LandPlot.Upgrade> UpgradesList { get; set; }
        private Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> UpgradesSet { get; set; }
        public readonly Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> Upgrades => UpgradesSet;

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteString(ID);
            writer.WriteEnum(Type);

            writer.WriteInt(UpgradesList.Count);
            foreach (var upgrade in UpgradesList)
                writer.WriteByte((byte)upgrade);
        }

        public void Deserialise(PacketReader reader)
        {
            ID = reader.ReadString();
            Type = reader.ReadEnum<LandPlot.Id>();

            UpgradesList = new Il2CppSystem.Collections.Generic.List<LandPlot.Upgrade>();
            UpgradesSet  = new Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade>();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                var upgrade = (LandPlot.Upgrade)reader.ReadByte();
                UpgradesList.Add(upgrade);
                UpgradesSet.Add(upgrade);
            }
        }
    }

    public byte Type { get; set; }
    public List<Plot> Plots { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteList(Plots, (writer, value) => value.Serialise(writer));
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        Plots = reader.ReadList(reader => reader.ReadPacket<Plot>());
    }
}