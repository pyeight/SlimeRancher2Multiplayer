using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Client;
using SR2MP.Client.Handlers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

[PacketHandler((byte)PacketType.InitialPediaEntries)]
public sealed class PediaLoadHandler : BaseClientPacketHandler
{
    public PediaLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PediasPacket>();
        
        SrLogger.LogError("Received PediaLoad packet");

        var unlocked = packet.Entries.ConvertAll(entry =>
            GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._pediaEntryLookup[entry]);
        
        SrLogger.LogMessage($"Received {packet.Entries.Count} entries in packet");
        
        var unlockedCpp = new Il2CppReferenceArray<PediaEntry>(unlocked.ToArray());
        SceneContext.Instance.PediaDirector._pediaModel.unlocked = Il2CppSystem.Linq.Enumerable.ToHashSet(
            unlockedCpp.Cast<CppCollections.IEnumerable<PediaEntry>>());
    }
}