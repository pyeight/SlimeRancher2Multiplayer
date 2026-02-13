using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using Enumerable = Il2CppSystem.Linq.Enumerable;

namespace SR2MP.Client.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.InitialPediaEntries)]
public sealed class InitialSlimePediaLoadHandler : BaseClientPacketHandler<InitialPediaPacket>
{
    public InitialSlimePediaLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialPediaPacket packet)
    {
        var unlocked = packet.Entries.ConvertAll(entry =>
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._pediaEntryLookup[entry]);

        var unlockedCpp = new Il2CppReferenceArray<PediaEntry>(unlocked.ToArray());
        SceneContext.Instance.PediaDirector._pediaModel.unlocked = Enumerable.ToHashSet(
            unlockedCpp.Cast<CppCollections.IEnumerable<PediaEntry>>());
    }
}