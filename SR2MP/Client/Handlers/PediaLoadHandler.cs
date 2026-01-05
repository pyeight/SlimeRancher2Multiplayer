using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using Enumerable = Il2CppSystem.Linq.Enumerable;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialPediaEntries)]
public sealed class PediaLoadHandler : BaseClientPacketHandler
{
    public PediaLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void HandleClient(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PediasPacket>();

        SrLogger.LogPacketSize("Received PediaLoad packet");

        var unlocked = packet.Entries.ConvertAll(entry =>
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._pediaEntryLookup[entry]);

        SrLogger.LogPacketSize($"Received {packet.Entries.Count} entries in packet");

        var unlockedCpp = new Il2CppReferenceArray<PediaEntry>(unlocked.ToArray());
        SceneContext.Instance.PediaDirector._pediaModel.unlocked = Enumerable.ToHashSet(
            unlockedCpp.Cast<CppCollections.IEnumerable<PediaEntry>>());
    }
}