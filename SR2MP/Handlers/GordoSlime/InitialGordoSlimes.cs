using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.InitialGordoSlimes, HandlerType.Client)]
internal sealed class InitialGordoSlimeLoadHandler : BasePacketHandler<InitialGordoSlimesPacket>
{
    protected override bool Handle(InitialGordoSlimesPacket packet, IPEndPoint? _)
    {
        foreach (var gordoSlime in packet.GordoSlimes)
        {
            if (GameState.gordos.TryGetValue(gordoSlime.Id, out var gordoModel))
            {
                gordoModel.GordoEatenCount = gordoSlime.EatenCount;
                gordoModel.targetCount = gordoSlime.RequiredEatCount;
            }
            else
            {
                gordoModel = new GordoModel
                {
                    fashions = new CppCollections.List<IdentifiableType>(0),
                    gordoEatCount = gordoSlime.EatenCount,
                    gordoSeen = false,
                    identifiableType = ActorManager.ActorTypes[gordoSlime.GordoSlimeType],
                    gameObj = null,
                    targetCount = gordoSlime.RequiredEatCount
                };

                GameState.gordos.Add(gordoSlime.Id, gordoModel);
            }

            if (gordoSlime.Popped)
            {
                NetworkGordoSlimeManager.MarkPopped(gordoSlime.Id);
                NetworkGordoSlimeManager.MarkRewarded(gordoSlime.Id);
            }
            
            gordoModel.gordoSeen = gordoSlime.WasSeen && !gordoSlime.Popped;

            NetworkGordoSlimeManager.ApplyState(gordoSlime.Id, gordoModel);
        }

        return false;
    }
}