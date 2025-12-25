using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.S2C
{
    public struct LandPlotsPacket : IPacket
    {
        public struct Plot
        {
            public string ID { get; set; }
            public LandPlot.Id Type { get; set; }

            internal Il2CppSystem.Collections.Generic.List<LandPlot.Upgrade> UpgradesList { get; set; }
            private Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> UpgradesSet { get; set; }
            public Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> Upgrades => UpgradesSet;

            public readonly void Serialize(PacketWriter writer)
            {
                writer.WriteString(ID);
                writer.WriteEnum(Type);

                writer.WriteInt(UpgradesList.Count);
                foreach (var upgrade in UpgradesList)
                    writer.WriteByte((byte)upgrade);
            }

            public void Deserialize(PacketReader reader)
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

                SrLogger.LogMessage($"ID: {ID} - Upgrades: {count}");
            }
        }

        public byte Type { get; set; }
        public List<Plot> Plots { get; set; }

        public readonly void Serialise(PacketWriter writer)
        {
            writer.WriteByte(Type);
            writer.WriteInt(Plots.Count);

            foreach (var plot in Plots)
                plot.Serialize(writer);
        }

        public void Deserialise(PacketReader reader)
        {
            Type = reader.ReadByte();

            int plotCount = reader.ReadInt();
            Plots = new List<Plot>(plotCount);

            for (int i = 0; i < plotCount; i++)
            {
                var plot = new Plot();
                plot.Deserialize(reader);
                Plots.Add(plot);
            }
        }
    }
}
