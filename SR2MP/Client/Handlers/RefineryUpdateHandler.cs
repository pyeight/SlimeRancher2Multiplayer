using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.RefineryUpdate)]
public sealed class RefineryUpdateHandler : BaseClientPacketHandler<RefineryUpdatePacket>
{
    public RefineryUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(RefineryUpdatePacket packet)
    {
        if (!actorManager.ActorTypes.TryGetValue(packet.ItemID, out var identType))
            return;

        handlingPacket = true;
        SceneContext.Instance.GadgetDirector._model.SetCount(identType, packet.ItemCount);
        handlingPacket = false;
    }
}