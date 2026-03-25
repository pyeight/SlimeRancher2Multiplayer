using System.Net;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using Enumerable = Il2CppSystem.Linq.Enumerable;

namespace SR2MP.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.InitialPediaEntries, HandlerType.Client)]
internal sealed class InitialSlimePediaLoadHandler : BasePacketHandler<InitialPediaPacket>
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