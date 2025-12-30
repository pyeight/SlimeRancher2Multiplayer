using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class MarketUpdatePacket : IPacket
    {
        public PacketType Type => PacketType.MarketUpdate;

        public System.Collections.Generic.Dictionary<string, float> Prices;

        public MarketUpdatePacket() { }

        public MarketUpdatePacket(System.Collections.Generic.Dictionary<string, float> prices)
        {
            Prices = prices;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteInt(Prices.Count);
            foreach (var kvp in Prices)
            {
                writer.WriteString(kvp.Key);
                writer.WriteFloat(kvp.Value);
            }
        }

        public void Deserialise(PacketReader reader)
        {
            int count = reader.ReadInt();
            Prices = new System.Collections.Generic.Dictionary<string, float>();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                float val = reader.ReadFloat();
                Prices[key] = val;
            }
        }
    }
}
