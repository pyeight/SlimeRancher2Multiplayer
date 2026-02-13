using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.Refinery;

[PacketHandler((byte)PacketType.RefineryUpdate)]
public sealed class RefineryUpdateHandler : BasePacketHandler<RefineryUpdatePacket>
{
    public RefineryUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(RefineryUpdatePacket packet, IPEndPoint clientEp)
    {
        if (!actorManager.ActorTypes.TryGetValue(packet.ItemID, out var identType))
            return;

        handlingPacket = true;
        SceneContext.Instance.GadgetDirector._model.SetCount(identType, packet.ItemCount);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}