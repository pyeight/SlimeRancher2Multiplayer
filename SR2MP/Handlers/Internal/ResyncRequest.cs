using System.Net;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.ResyncRequest, HandlerType.Server)]
internal sealed class ResyncRequestHandler : BasePacketHandler<ResyncRequestPacket>
{
    protected override bool Handle(ResyncRequestPacket packet, IPEndPoint? clientEp)
    {
        if (clientEp == null)
            return false;

        if (!Main.Server.ClientManager.TryGetClient(clientEp, out var clientInfo))
        {
            SrLogger.LogWarning($"Resync requested for unknown endpoint: {clientEp}");
            return false;
        }

        var resyncManager = Main.Server.ReSyncManager;

        if (!resyncManager.CanResync(clientEp))
        {
            resyncManager.SendCooldownMessage(clientEp);
            return false;
        }

        resyncManager.MarkResynced(clientEp);
        ReSyncManager.SynchronizeClient(clientInfo!.PlayerId, clientEp);
        resyncManager.LogResyncRequest(clientInfo.PlayerId, clientEp);
        resyncManager.SendSuccessMessage(clientEp);

        return false;
    }
}
