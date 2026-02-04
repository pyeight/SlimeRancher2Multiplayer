using Il2CppMonomiPark.SlimeRancher.Event;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialMapEntries)]
public sealed class InitialMapLoadHandler : BaseClientPacketHandler<InitialMapPacket>
{
    public InitialMapLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialMapPacket packet)
    {
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