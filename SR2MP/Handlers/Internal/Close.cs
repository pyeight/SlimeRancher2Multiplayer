using System.Net;
using SR2MP.Packets;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.Close, HandlerType.Client)]
internal sealed class CloseHandler : BasePacketHandler<ClosePacket>
{
    protected override bool Handle(ClosePacket packet, IPEndPoint? clientEp)
    {
        SrLogger.LogMessage("Server closed, disconnecting!");
        Main.Client.Disconnect();
        return false;
    }
}