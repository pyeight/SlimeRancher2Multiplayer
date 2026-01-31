using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Pedia;

[SR2MP.Networking.NetDelivery(LiteNetLib.DeliveryMethod.ReliableOrdered, channel: SR2MP.Networking.NetChannels.WorldState)]
public sealed class PediaUnlockPacket : PacketBase
{
    public string ID { get; set; }
    public bool Popup { get; set; }

    public override PacketType Type => PacketType.PediaUnlock;

    public override void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WriteBool(Popup);
    }

    public override void Deserialise(PacketReader reader)
    {
        ID = reader.ReadString();
        Popup = reader.ReadBool();
    }
}