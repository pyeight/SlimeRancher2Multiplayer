using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialGordos)]
public sealed class GordosLoadHandler : BaseClientPacketHandler
{
    public GordosLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GordosPacket>();
        
        var gameModel = SceneContext.Instance.GameModel;
        
        foreach (var gordo in packet.Gordos)
        {
            if (gameModel.gordos.TryGetValue(gordo.Id, out var gordoModel))
            {
                gordoModel.GordoEatenCount = gordo.EatenCount;
                gordoModel.targetCount = gordo.RequiredEatCount;

                if (gordoModel.gameObj)
                {
                    var gordoComponent = gordoModel.gameObj.GetComponent<GordoEat>();
                    
                    gordoComponent.SetModel(gordoModel);
                }
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