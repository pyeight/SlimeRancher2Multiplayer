using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoBurst)]
public sealed class GordoSlimeBurstHandler : BaseClientPacketHandler<GordoBurstPacket>
{
    public GordoSlimeBurstHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(GordoBurstPacket packet)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordoSlime))
        {
            gordoSlime.GordoEatenCount = gordoSlime.targetCount + 1;

            handlingPacket = true;
            if (gordoSlime.gameObj)
                gordoSlime.gameObj.GetComponent<GordoEat>().ImmediateReachedTarget();
            handlingPacket = false;
        }
        else
        {
            gordoSlime = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordoSlime);
        }
    }
}