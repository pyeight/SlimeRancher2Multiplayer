using System.Collections.Generic;
using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared;

public struct InitialGadgetsPacket : IPacket
{
    public byte Type { get; set; }
    public List<GadgetPacket> Gadgets { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteInt(Gadgets.Count);
        foreach (var gadget in Gadgets)
        {
            gadget.Serialise(writer);
        }
    }

    public void Deserialise(PacketReader reader)
    {
        var count = reader.ReadInt();
        Gadgets = new List<GadgetPacket>(count);
        for (int i = 0; i < count; i++)
        {
            var gadget = new GadgetPacket();
            gadget.Deserialise(reader);
            Gadgets.Add(gadget);
        }
    }
}
