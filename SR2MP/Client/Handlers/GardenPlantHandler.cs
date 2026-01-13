using SR2MP.Packets.Landplot;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GardenPlant)]
public sealed class GardenPlantHandler : BaseClientPacketHandler
{
    public GardenPlantHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GardenPlantPacket>();

        var model = SceneContext.Instance.GameModel.landPlots[packet.ID];

        model.resourceGrowerDefinition =
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation.RawLookupDictionary._entries.FirstOrDefault(x =>
                x.value._primaryResourceType == actorManager.ActorTypes[packet.ActorType])!.value;
        
        
    }
}