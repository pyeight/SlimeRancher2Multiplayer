using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorTransfer)]
internal sealed class ActorTransferHandler : BasePacketHandler<ActorTransferPacket>
{
    protected override bool Handle(ActorTransferPacket packet, IPEndPoint? _)
    {
        NetworkActorManager.ApplyOwnership(packet);

        return true;
    }
}