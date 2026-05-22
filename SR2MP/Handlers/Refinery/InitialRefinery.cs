using System.Collections;
using System.Net;
using MelonLoader;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Refinery;

[PacketHandler((byte)PacketType.InitialRefinery, HandlerType.Client)]
internal sealed class InitialRefineryHandler : BasePacketHandler<InitialRefineryPacket>
{
    protected override bool Handle(InitialRefineryPacket packet, IPEndPoint? _)
    {
        StartCoroutine(InitializeRefinery(packet));
        return false;
    }

    private static IEnumerator InitializeRefinery(InitialRefineryPacket packet)
    {
        HandlingPacket = true;

        var newItemCounts = new CppCollections.Dictionary<IdentifiableType, int>();

        foreach (var item in packet.Items)
        {
            if (ActorManager.ActorTypes.TryGetValue(item.Key, out var identType))
            {
                newItemCounts.Add(identType, item.Value);
            }
            yield return null;
        }

        yield return null;

        SceneContext.Instance.GadgetDirector._model._itemCounts = newItemCounts;

        HandlingPacket = false;
    }
}