using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;
using SR2MP.Packets.Player;

namespace SR2MP.Shared.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerGadgetUpdate)]
public sealed class PlayerGadgetUpdate : BasePacketHandler<PlayerGadgetUpdatePacket>
{
    protected override bool Handle(PlayerGadgetUpdatePacket packet, IPEndPoint? _)
    {
        handlingPacket = true;
        
        handlingPacket = false;

        return true;
    }
}