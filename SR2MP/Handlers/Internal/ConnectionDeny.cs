using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.ConnectionDeny, HandlerType.Client)]
internal sealed class ConnectionDenyHandler : BasePacketHandler<ConnectionDenyPacket>
{
    protected override bool Handle(ConnectionDenyPacket packet, IPEndPoint? _)
    {
        Main.Client.UpdateConnectionStatus(false);
        MultiplayerUI.Instance.RegisterSystemMessage("Connection was denied!",
            $"SYSTEM_CONNECTION_DENY_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageClose);
        return false;
    }
}