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
        if (!PlayerObjects.TryGetValue(packet.PlayerId, out var obj)) return true;

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        var player = obj?.GetComponent<NetworkPlayer>();

        if (player == null) return true;

        player.OnlineGadgetMode = packet.Enabled;
        player.OnlineGadgetID = packet.CurrentGadget;
        player.OnlinePlacementValid = packet.ValidPlacement;

        if (packet.Enabled)
            player.OnGadgetPositionReceived(packet.Position, packet.Rotation, packet.GadgetLocalRotation);

        return true;
    }
}