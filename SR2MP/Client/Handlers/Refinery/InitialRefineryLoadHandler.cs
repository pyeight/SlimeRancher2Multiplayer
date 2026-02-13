using System.Collections;
using MelonLoader;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Refinery;

[PacketHandler((byte)PacketType.InitialRefinery)]
public sealed class InitialRefineryHandler : BaseClientPacketHandler<InitialRefineryPacket>
{
    public InitialRefineryHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialRefineryPacket packet)
    {
        MelonCoroutines.Start(InitializeRefinery(packet));
    }

    private static IEnumerator InitializeRefinery(InitialRefineryPacket packet)
    {
        handlingPacket = true;

        var newItemCounts = new CppCollections.Dictionary<IdentifiableType, int>();

        foreach (var item in packet.Items)
        {
            if (actorManager.ActorTypes.TryGetValue(item.Key, out var identType))
            {
                newItemCounts.Add(identType, item.Value);
            }
            yield return null;
        }

        yield return null;

        SceneContext.Instance.GadgetDirector._model._itemCounts = newItemCounts;

        handlingPacket = false;
    }
}