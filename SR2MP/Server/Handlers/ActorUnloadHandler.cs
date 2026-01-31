using LiteNetLib;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorUnload)]
public sealed class ActorUnloadHandler : BasePacketHandler<ActorUnloadPacket>
{
    public ActorUnloadHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(ActorUnloadPacket packet, NetPeer clientPeer)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;

        if (!component.regionMember)
            return;

        if (!component.regionMember._hibernating)
        {
            component.LocallyOwned = true;

            var ownershipPacket = new ActorTransferPacket
            {
                ActorId = packet.ActorId,
                OwnerPlayer = LocalID,
            };
            Main.SendToAllOrServer(ownershipPacket);
            return;
        }

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}