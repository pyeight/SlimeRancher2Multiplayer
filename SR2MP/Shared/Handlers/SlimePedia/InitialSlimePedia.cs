using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;
using Enumerable = Il2CppSystem.Linq.Enumerable;

namespace SR2MP.Shared.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.InitialPediaEntries, HandlerType.Client)]
public sealed class InitialSlimePediaLoadHandler : BasePacketHandler<InitialPediaPacket>
{
    protected override bool Handle(InitialPediaPacket packet, IPEndPoint? _)
    {
        var unlocked = packet.Entries.ConvertAll(entry =>
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._pediaEntryLookup[entry]);

        var unlockedCpp = new Il2CppReferenceArray<PediaEntry>(unlocked.ToArray());
        SceneContext.Instance.PediaDirector._pediaModel.unlocked = Enumerable.ToHashSet(
            unlockedCpp.Cast<CppCollections.IEnumerable<PediaEntry>>());

        return false;
    }
}