using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Gadget)]
public class GadgetHandler : BasePacketHandler
{
    public GadgetHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, string clientIdentifier)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GadgetPacket>();
        
        // Broadcast to all other clients
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        
        // Use injected network manager or Main.Server
        Main.Server.SendToAllExcept(packet, clientIdentifier);
    }
}
