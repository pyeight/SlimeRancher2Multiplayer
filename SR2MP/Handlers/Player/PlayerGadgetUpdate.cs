using System.Net;
using SR2MP.Components.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerGadgetUpdate)]
internal sealed class PlayerGadgetUpdate : BasePacketHandler<PlayerGadgetUpdatePacket>
{
    protected override bool Handle(PlayerGadgetUpdatePacket packet, IPEndPoint? _)
    {
        if (packet.Enabled)
        {
            PlayerManager.UpdatePlayerGadget(
                packet.PlayerId,
                packet.Enabled,
                packet.CurrentGadget,
                packet.ValidPlacement,
                packet.Position,
                packet.Rotation,
                packet.GadgetLocalRotation);
        }
        else
        {
            PlayerManager.UpdatePlayerGadget(
                packet.PlayerId,
                packet.Enabled);
        }
        
        return true;
    }
}