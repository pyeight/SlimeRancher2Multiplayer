using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.InitialGordos, HandlerType.Client)]
public sealed class InitialGordoSlimeLoadHandler : BasePacketHandler<InitialGordosPacket>
{
    public InitialGordoSlimeLoadHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(InitialGordosPacket packet, IPEndPoint? _)
    {
        var gameModel = SceneContext.Instance.GameModel;

        foreach (var gordoSlime in packet.GordoSlimes)
        {
            if (gameModel.gordos.TryGetValue(gordoSlime.Id, out var gordoModel))
            {
                gordoModel.GordoEatenCount = gordoSlime.EatenCount;
                gordoModel.targetCount = gordoSlime.RequiredEatCount;

                if (!gordoModel.gameObj)
                    continue;

                var gordoComponent = gordoModel.gameObj.GetComponent<GordoEat>();
                gordoComponent.SetModel(gordoModel);
                gordoModel.gameObj.SetActive(gordoSlime.EatenCount < gordoSlime.RequiredEatCount);
            }
            else
            {
                gordoModel = new GordoModel
                {
                    fashions = new CppCollections.List<IdentifiableType>(0),
                    gordoEatCount = gordoSlime.EatenCount,
                    gordoSeen = false,
                    identifiableType = actorManager.ActorTypes[gordoSlime.GordoSlimeType],
                    gameObj = null,
                    targetCount = gordoSlime.RequiredEatCount
                };

                gameModel.gordos.Add(gordoSlime.Id, gordoModel);
            }
        }

        return false;
    }
}