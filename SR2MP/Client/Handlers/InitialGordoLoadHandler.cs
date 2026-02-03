using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialGordos)]
public sealed class GordosLoadHandler : BaseClientPacketHandler<InitialGordosPacket>
{
    public GordosLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialGordosPacket packet)
    {
        var gameModel = SceneContext.Instance.GameModel;

        foreach (var gordo in packet.Gordos)
        {
            if (gameModel.gordos.TryGetValue(gordo.Id, out var gordoModel))
            {
                gordoModel.GordoEatenCount = gordo.EatenCount;
                gordoModel.targetCount = gordo.RequiredEatCount;

                if (!gordoModel.gameObj)
                    continue;
                var gordoComponent = gordoModel.gameObj.GetComponent<GordoEat>();

                gordoComponent.SetModel(gordoModel);

                gordoModel.gameObj.SetActive(gordo.EatenCount < gordo.RequiredEatCount);
            }
            else
            {
                gordoModel = new GordoModel
                {
                    fashions = new CppCollections.List<IdentifiableType>(0),
                    gordoEatCount = gordo.EatenCount,
                    gordoSeen = false,
                    identifiableType = actorManager.ActorTypes[gordo.GordoType],
                    gameObj = null,
                    targetCount = gordo.RequiredEatCount,
                };

                gameModel.gordos.Add(gordo.Id, gordoModel);
            }
        }
    }
}