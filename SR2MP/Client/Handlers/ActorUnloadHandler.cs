using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorUnload)]
public sealed class ActorUnloadHandler : BaseClientPacketHandler
{
    public ActorUnloadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorUnloadPacket>();

        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;

        if (!component.regionMember || component.regionMember._hibernating)
            return;

        component.LocallyOwned = true;

        var ownershipPacket = new ActorTransferPacket
        {
            ActorId = packet.ActorId,
            OwnerPlayer = LocalID,
        };
        Main.SendToAllOrServer(ownershipPacket);
    }
}