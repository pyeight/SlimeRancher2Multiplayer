using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.MapUnlock)]
public sealed class MapUnlockHandler : BaseClientPacketHandler
{
    public MapUnlockHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MapUnlockPacket>();
        
        var gameEvent = Resources.FindObjectsOfTypeAll<StaticGameEvent>().FirstOrDefault(x => x._dataKey == packet.NodeID);
        SceneContext.Instance.MapDirector.NotifyZoneUnlocked(gameEvent, false, 0);

        var activator = Resources.FindObjectsOfTypeAll<MapNodeActivator>().FirstOrDefault(x => x._fogRevealEvent._dataKey == packet.NodeID);
        activator?.StartCoroutine(activator.ActivateHologramAnimation());
        
        
        var eventDirModel = SceneContext.Instance.eventDirector._model;
        if (!eventDirModel.table.TryGetValue(MapEventKey, out var table))
        {
            eventDirModel.table.Add(MapEventKey,
                new CppCollections.Dictionary<string, EventRecordModel.Entry>());
            table = eventDirModel.table[MapEventKey];
        }

        table[packet.NodeID] = new EventRecordModel.Entry
        {
            count = 1,
            createdRealTime = 0,
            createdGameTime = 0,
            dataKey = packet.NodeID,
            eventKey = MapEventKey,
            updatedRealTime = 0,
            updatedGameTime = 0,
        };
    }
}