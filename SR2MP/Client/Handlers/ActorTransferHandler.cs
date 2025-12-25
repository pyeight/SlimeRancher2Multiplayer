using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorTransfer)]
public class ActorTransferHandler : BaseClientPacketHandler
{
    public ActorTransferHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorTransferPacket>();

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
    }
}