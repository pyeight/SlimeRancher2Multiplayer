using Il2CppMonomiPark.SlimeRancher.Event;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Map;

[PacketHandler((byte)PacketType.InitialMapEntries, HandlerType.Client)]
public sealed class InitialMapLoadHandler : BasePacketHandler<InitialMapPacket>
{
    protected override bool Handle(InitialMapPacket packet, IPEndPoint? _)
    {
        var eventModel = SceneContext.Instance.eventDirector._model;
        var dict = eventModel.table[MapEventKey] = new CppCollections.Dictionary<string, EventRecordModel.Entry>();

        foreach (var node in packet.UnlockedNodes)
        {
            dict[node] = new EventRecordModel.Entry
            {
                count = 1,
                createdRealTime = 0,
                createdGameTime = 0,
                dataKey = node,
                eventKey = MapEventKey,
                updatedRealTime = 0,
                updatedGameTime = 0
            };
        }

        return false;
    }
}