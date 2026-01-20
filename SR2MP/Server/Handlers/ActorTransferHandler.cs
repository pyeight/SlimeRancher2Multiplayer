using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorTransfer)]
public sealed class ActorTransferHandler : BasePacketHandler<ActorTransferPacket>
{
    public ActorTransferHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(ActorTransferPacket packet, IPEndPoint clientEp)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;

        var vac = SceneContext.Instance.Player.GetComponent<PlayerItemController>()._vacuumItem;
        var gameObject = actor.GetGameObject();
        if (vac._held == gameObject)
        {
            vac.LockJoint.connectedBody = null;
            vac._held = null;
            vac.SetHeldRad(0f);
            vac._vacMode = VacuumItem.VacMode.NONE;
            gameObject.GetComponent<Vacuumable>().Release();
        }

        component.LocallyOwned = false;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}