using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorTransfer)]
public sealed class ActorTransferHandler : BasePacketHandler<ActorTransferPacket>
{
    protected override bool Handle(ActorTransferPacket packet, IPEndPoint? _)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return false;

        if (!actor.TryGetNetworkComponent(out var component))
            return false;

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

        return true;
    }
}