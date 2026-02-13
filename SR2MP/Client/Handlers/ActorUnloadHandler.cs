using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorUnload)]
public sealed class ActorUnloadHandler : BaseClientPacketHandler<ActorUnloadPacket>
{
    public ActorUnloadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ActorUnloadPacket packet)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;

        if (!component.regionMember || component.regionMember!._hibernating)
            return;

        component.LocallyOwned = true;

        var ownershipPacket = new ActorTransferPacket
        {
            ActorId = packet.ActorId,
            OwnerId = LocalID
        };
        Main.SendToAllOrServer(ownershipPacket);
    }
}