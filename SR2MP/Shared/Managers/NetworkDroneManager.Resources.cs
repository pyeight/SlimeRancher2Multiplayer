using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.Loading;
using SR2MP.Packets.World;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkDroneManager
{
    internal static InitialDroneResourcesPacket CreateInitialPacket()
    {
        var packet = new InitialDroneResourcesPacket();

        foreach (var (sceneId, group) in NetworkSceneManager.GetAllSceneGroups())
        {
            try
            {
                var store = group?.DiscoverableResources;
                var data = store?._resourceData;
                if (data == null || data.Length == 0)
                    continue;

                var scene = new InitialDroneResourcesPacket.SceneEntries { Scene = sceneId };

                foreach (var slot in data)
                    scene.Entries.Add(CreateEntry(slot));

                packet.Scenes.Add(scene);
            }
            catch (Exception ex)
            {
                SrLogger.LogDebug($"Failed to get drone resources for scene {sceneId}: {ex.Message}");
            }
        }

        return packet;
    }

    internal static DroneResourcePacket.DroneResourceEntry CreateEntry(DiscoverableResourceData? data)
    {
        if (data?._initialized != true)
            return new DroneResourcePacket.DroneResourceEntry();

        var entry = new DroneResourcePacket.DroneResourceEntry
        {
            Valid = true,
            Id = data._id ?? string.Empty,
            Location = data._location,
            OffsetLocation = data._offsetLocation
        };

        var types = data._identifiableTypes;
        if (types != null)
        {
            foreach (var type in types)
            {
                if (type != null)
                    entry.TypeIds.Add(NetworkActorManager.GetPersistentID(type));
            }
        }

        return entry;
    }

    internal static void ApplyInitialResources(InitialDroneResourcesPacket packet)
    {
        foreach (var scene in packet.Scenes)
        {
            try
            {
                var store = NetworkSceneManager.GetSceneGroup(scene.Scene)?.DiscoverableResources;
                if (store == null)
                    continue;

                var data = new CppCollections.List<DiscoverableResourceData>(scene.Entries.Count);

                foreach (var entry in scene.Entries)
                    data.Add(ToResourceData(entry));

                store.SetAllDiscoverableResourceData(data);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to apply discoverable resources for scene {scene.Scene}: {ex.Message}");
            }
        }
    }

    internal static DiscoverableResourceData ToResourceData(DroneResourcePacket.DroneResourceEntry entry)
    {
        var types = new CppCollections.List<IdentifiableType>();

        foreach (var typeId in entry.TypeIds)
        {
            if (ActorManager.ActorTypes.TryGetValue(typeId, out var type) && type != null)
                types.Add(type);
        }

        var data = new DiscoverableResourceData(entry.Id, entry.Location, entry.OffsetLocation, types)
        {
            _initialized = entry.Valid
        };

        return data;
    }
}
