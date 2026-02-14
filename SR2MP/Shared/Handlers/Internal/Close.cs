using System.Net;
using SR2MP.Packets;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Internal;

[PacketHandler((byte)PacketType.Close, HandlerType.Client)]
public sealed class CloseHandler : BasePacketHandler<ClosePacket>
{
    protected override bool Handle(ClosePacket packet, IPEndPoint? clientEp)
    {
        SrLogger.LogMessage("Server closed, disconnecting!", SrLogTarget.Both);
        Main.Client.Disconnect();
        return false;
    }
}