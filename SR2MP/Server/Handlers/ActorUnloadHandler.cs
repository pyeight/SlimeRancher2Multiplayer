using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorUnload)]
public sealed class ActorUnloadHandler : BasePacketHandler
{
    public ActorUnloadHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorUnloadPacket>();

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
                Type = (byte)PacketType.ActorTransfer,
                ActorId = packet.ActorId,
                OwnerPlayer = LocalID,
            };
            Main.SendToAllOrServer(ownershipPacket);
            return;
        }

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}