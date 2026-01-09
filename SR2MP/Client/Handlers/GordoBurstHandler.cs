using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoBurstHandler : BaseClientPacketHandler
{
    public GordoBurstHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GordoFeedPacket>();

        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = gordo.targetCount + 1;
        }
        else
        {
            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50,
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordo);
        }
    }
}