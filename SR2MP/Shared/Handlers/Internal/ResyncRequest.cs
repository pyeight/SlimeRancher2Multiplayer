using System.Net;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Internal;

[PacketHandler((byte)PacketType.ResyncRequest, HandlerType.Server)]
public sealed class ResyncRequestHandler : BasePacketHandler<ResyncRequestPacket>
{
    protected override bool Handle(ResyncRequestPacket packet, IPEndPoint? clientEp)
    {
        if (clientEp == null)
            return false;

        if (!Main.Server.clientManager.TryGetClient(clientEp, out var clientInfo))
        {
            SrLogger.LogWarning($"Resync requested for unknown endpoint: {clientEp}", SrLogTarget.Both);
            return false;
        }

        var resyncManager = Main.Server.reSyncManager;

        if (!resyncManager.CanResync(clientEp))
        {
            resyncManager.SendCooldownMessage(clientEp);
            return false;
        }

        resyncManager.MarkResynced(clientEp);
        resyncManager.SynchronizeClient(clientInfo!.PlayerId, clientEp);
        resyncManager.SendSuccessMessage(clientEp);

        return false;
    }
}
