using SR2MP.Components.LandPlots;
using SR2MP.Packets.LandPlots;

namespace SR2MP.Shared.Managers;

internal static class NetworkGardenManager
{
    private static readonly Dictionary<string, NetworkGarden> Gardens = new();
    
    private static readonly Dictionary<string, string> KnownOwners = new();

    private static readonly HashSet<string> PendingClaims = new();
    private static readonly Dictionary<string, GardenUpdatePacket.Entry> PendingFinalStates = new();

    private static int lastTickFrame = -1;
    private static float nextClaimFlush;
    private static float nextStateFlush;
    private const float ClaimFlushInterval = 1f;
    private const float StateFlushInterval = 5f;

    internal static void OnServerStarted()
    {
        KnownOwners.Clear();
        PendingClaims.Clear();
        PendingFinalStates.Clear();

        foreach (var spawnResource in Object.FindObjectsOfType<SpawnResource>(true))
        {
            if (spawnResource.GetComponent<NetworkGarden>() == null)
                spawnResource.gameObject.AddComponent<NetworkGarden>();
        }

        var claimed = 0;
        foreach (var garden in Gardens.Values)
        {
            if (!garden || garden.IsHibernated)
                continue;

            garden.TakeOwnership(broadcast: false);
            claimed++;
        }

        SrLogger.LogDebug($"Host took ownership of {claimed} registered gardens on server start.");
    }

    internal static void OnDisconnected()
    {
        KnownOwners.Clear();
        PendingClaims.Clear();
        PendingFinalStates.Clear();

        foreach (var garden in Gardens.Values)
        {
            if (garden)
                garden.ResetToVanilla();
        }
    }

    internal static void Register(NetworkGarden garden)
    {
        var id = garden.Id!;
        Gardens[id] = garden;

        if (Main.Server.IsRunning &&
            KnownOwners.TryGetValue(id, out var owner) &&
            owner != LocalID &&
            PlayerManager.CheckPlayerExists(owner))
        {
            garden.SetOwner(owner);
        }
    }

    internal static void Unregister(NetworkGarden garden)
    {
        if (garden.Id != null && Gardens.TryGetValue(garden.Id, out var registered) && registered == garden)
            Gardens.Remove(garden.Id);
    }

    internal static void QueueClaim(string gardenId)
        => PendingClaims.Add(gardenId);

    internal static void QueueFinalState(NetworkGarden garden)
    {
        var entry = garden.CaptureState();
        if (entry != null)
            PendingFinalStates[entry.Value.GardenId] = entry.Value;
    }

    internal static void ApplyOwnership(string gardenId, string ownerId)
    {
        if (Main.Server.IsRunning)
            KnownOwners[gardenId] = ownerId;

        if (!Gardens.TryGetValue(gardenId, out var garden) || !garden)
            return;

        // An empty owner is a release (the owner hibernated)
        // Others are free to take over
        if (string.IsNullOrEmpty(ownerId))
            garden.OnOwnerReleased();
        else
            garden.SetOwner(ownerId);
    }
    
    internal static void BroadcastRelease(string gardenId)
    {
        if (Main.Server.IsRunning)
            KnownOwners[gardenId] = string.Empty;

        Main.SendToAllOrServer(new GardenOwnershipPacket
        {
            Entries = new List<GardenOwnershipPacket.Entry> { new() { GardenId = gardenId, OwnerId = string.Empty } }
        });
    }

    internal static void ApplyState(GardenUpdatePacket.Entry entry)
    {
        var found = Gardens.TryGetValue(entry.GardenId, out var garden) && garden;
        if (found)
            garden!.ApplyUpdate(entry.NextSpawnTime, entry.StoredWater, entry.NextSpawnRipens);
    }
    
    internal static void Tick()
    {
        if (lastTickFrame == Time.frameCount)
            return;
        lastTickFrame = Time.frameCount;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            return;

        var now = Time.time;

        if (now >= nextClaimFlush)
        {
            nextClaimFlush = now + ClaimFlushInterval;
            FlushClaims();
        }

        if (now >= nextStateFlush)
        {
            nextStateFlush = now + StateFlushInterval;
            FlushStates();
        }
    }

    private static void FlushClaims()
    {
        if (PendingClaims.Count == 0)
            return;

        var entries = new List<GardenOwnershipPacket.Entry>(PendingClaims.Count);
        foreach (var id in PendingClaims)
        {
            entries.Add(new GardenOwnershipPacket.Entry { GardenId = id, OwnerId = LocalID });

            if (Main.Server.IsRunning)
                KnownOwners[id] = LocalID;
        }
        PendingClaims.Clear();

        Main.SendToAllOrServer(new GardenOwnershipPacket { Entries = entries });
    }

    private static void FlushStates()
    {
        List<GardenUpdatePacket.Entry>? entries = null;

        foreach (var garden in Gardens.Values)
        {
            if (!garden || !garden.LocallyOwned || garden.IsHibernated)
                continue;

            var entry = garden.CaptureState();
            if (entry != null)
                (entries ??= new()).Add(entry.Value);
        }

        if (PendingFinalStates.Count > 0)
        {
            entries ??= new();
            entries.AddRange(PendingFinalStates.Values);
            PendingFinalStates.Clear();
        }

        if (entries != null)
            Main.SendToAllOrServer(new GardenUpdatePacket { Entries = entries });
    }
    
    internal static void AssignOwnershipOfUnowned()
    {
        if (!Main.Server.IsRunning)
            return;

        if (SceneContext.Instance?.player == null)
            return;

        var allPlayers = new List<(string PlayerId, Vector3 Position)>
        {
            (LocalID, SceneContext.Instance.player.transform.position)
        };

        foreach (var (playerId, playerObject) in PlayerObjects)
        {
            if (playerObject)
                allPlayers.Add((playerId, playerObject.transform.position));
        }

        List<GardenOwnershipPacket.Entry>? entries = null;

        foreach (var (gardenId, owner) in KnownOwners.ToArray())
        {
            if (owner == LocalID || PlayerManager.CheckPlayerExists(owner))
                continue;

            Gardens.TryGetValue(gardenId, out var garden);

            var newOwner = LocalID;

            if (garden)
            {
                var position = garden!.transform.position;
                var bestDistance = float.MaxValue;

                foreach (var (playerId, playerPosition) in allPlayers)
                {
                    var distance = (playerPosition - position).sqrMagnitude;

                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    newOwner = playerId;
                }
            }

            KnownOwners[gardenId] = newOwner;

            if (garden)
                garden!.SetOwner(newOwner);

            (entries ??= new()).Add(new GardenOwnershipPacket.Entry { GardenId = gardenId, OwnerId = newOwner });
        }

        if (entries != null)
            Main.SendToAllOrServer(new GardenOwnershipPacket { Entries = entries });
    }

    internal static void RestoreAfterTimeSkip()
    {
        foreach (var garden in Gardens.Values)
        {
            if (garden && !garden.LocallyOwned)
                garden.RestoreCachedState();
        }
    }
}