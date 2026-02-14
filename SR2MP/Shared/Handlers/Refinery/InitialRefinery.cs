using System.Collections;
using MelonLoader;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Refinery;

[PacketHandler((byte)PacketType.InitialRefinery, HandlerType.Client)]
public sealed class InitialRefineryHandler : BasePacketHandler<InitialRefineryPacket>
{
    protected override bool Handle(InitialRefineryPacket packet, IPEndPoint? _)
    {
        MelonCoroutines.Start(InitializeRefinery(packet));
        return false;
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