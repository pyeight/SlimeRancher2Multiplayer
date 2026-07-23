using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.PlortDepositor)]
internal sealed class PlortDepositorHandler : BasePacketHandler<PlortDepositorPacket>
{
    protected override bool Handle(PlortDepositorPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;
        NetworkDepositorManager.ApplyState(packet.ID, packet.AmountDeposited);
        HandlingPacket = false;

        return true;
    }
}
