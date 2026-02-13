using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoBurst)]
public sealed class GordoSlimeBurstHandler : BasePacketHandler<GordoBurstPacket>
{
    public GordoSlimeBurstHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GordoBurstPacket packet, IPEndPoint clientEp)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = gordo.targetCount + 1;

            handlingPacket = true;
            if (gordo.gameObj)
                gordo.gameObj.GetComponent<GordoEat>().ImmediateReachedTarget();
            handlingPacket = false;
        }
        else
        {
            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordo);
        }

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}