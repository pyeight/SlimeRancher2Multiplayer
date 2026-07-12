using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player.Component;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Refinery;

[PacketHandler((byte)PacketType.InitialComponents, HandlerType.Client)]
internal sealed class InitialComponentsHandler : BasePacketHandler<InitialComponentsPacket>
{
    protected override bool Handle(InitialComponentsPacket packet, IPEndPoint? _)
    {
        StartCoroutine(InitializeComponents(packet));
        return false;
    }

    private static IEnumerator InitializeComponents(InitialComponentsPacket packet)
    {
        yield return null;
        var model = GameState.PlayerModel.UpgradeComponentsModel;

        model._ownedComponents = new CppCollections.Dictionary<UpgradeComponent, int>();
        foreach (var (componentId, count) in packet.Items)
        {
            var comp = model._upgradeComponentList.items._items.First(x => x._referenceId == componentId);
            model._ownedComponents.Add(comp, count);
        }
    }
}