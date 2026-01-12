using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

using Enumerable = Il2CppSystem.Linq.Enumerable;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialMap)]
public sealed class MapLoadHandler : BaseClientPacketHandler
{
    public MapLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MapPacket>();

        var eventModel = SceneContext.Instance.eventDirector._model;

        eventModel.table[MapEventKey] = new CppCollections.Dictionary<string, EventRecordModel.Entry>();

        foreach (var node in packet.UnlockedNodes)
        {
            eventModel.table[MapEventKey][node] = new EventRecordModel.Entry
            {
                count = 1,
                createdRealTime = 0,
                createdGameTime = 0,
                dataKey = node,
                eventKey = MapEventKey,
                updatedRealTime = 0,
                updatedGameTime = 0,
            };
        }
    }
}