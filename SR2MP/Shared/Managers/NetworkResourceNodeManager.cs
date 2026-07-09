using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;
using SR2MP.Packets.World;

namespace SR2MP.Shared.Managers;

internal sealed class NetworkResourceNodeManager
{
    internal readonly HashSet<string> RemotelyHarvested = new();
    
    private readonly Dictionary<string, ResourceNodePlacement> nodePlacements = new();

    private bool initialized;

    internal ResourceNodePlacement CreatePlacement(ResourceNodeSpawnerModel model)
    {
        var placement = new ResourceNodePlacement
        {
            ID = model.nodeId,
            HasNode = model.resourceNodeDefinition != null,
            State = (byte)model.nodeState
        };

        if (!placement.HasNode)
            return placement;

        var definitions = Il2CppSystem.Linq.Enumerable.ToArray(model.resourceNodeDefinitions);
        for (var i = 0; i < definitions.Length; i++)
        {
            if (definitions[i] == model.resourceNodeDefinition)
            {
                placement.DefinitionIndex = i;
                break;
            }
        }

        placement.VariantIndex = model.resourceNodeVariantIndex;
        placement.DespawnAtWorldTime = model.despawnAtWorldTime;

        var resources = model.resourcesToSpawn;
        if (resources != null)
        {
            for (var i = 0; i < resources.Count; i++)
                placement.ResourcesToSpawn.Add(NetworkActorManager.GetPersistentID(resources[i]));
        }

        return placement;
    }

    internal void ApplyInitialPlacements(List<ResourceNodePlacement> nodes)
    {
        RemotelyHarvested.Clear();
        nodePlacements.Clear();

        foreach (var node in nodes)
            nodePlacements[node.ID] = node;

        EnsureLoopStarted();
        ApplyRemotePlacements();
    }

    internal void ApplyPlacement(ResourceNodePlacement placement)
    {
        nodePlacements[placement.ID] = placement;

        EnsureLoopStarted();
        SetPlacement(placement);
    }

    internal void ApplyState(string nodeId, ResourceNode.NodeState state)
    {
        if (state == ResourceNode.NodeState.HARVESTING)
            RemotelyHarvested.Add(nodeId);
        else
            RemotelyHarvested.Remove(nodeId);
        
        if (nodePlacements.TryGetValue(nodeId, out var placement))
            placement.State = (byte)state;

        var node = GetNode(nodeId);
        if (node == null || node._model == null)
        {
            SrLogger.LogDebug($"ResourceNode {nodeId} not found");
            return;
        }

        HandlingPacket = true;
        try
        {
            SetNodeState(node, state);
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"Failed to apply state {state} to resource node '{nodeId}': {ex.Message}");
        }
        finally
        {
            HandlingPacket = false;
        }
    }
    
    private static void SetNodeState(ResourceNode node, ResourceNode.NodeState state)
    {
        if (node._model != null)
            node._model.nodeState = state;

        switch (state)
        {
            case ResourceNode.NodeState.READY:
                node.SetStateReady();
                break;
            case ResourceNode.NodeState.HARVESTING:
                node.SetStateHarvesting();
                break;
            case ResourceNode.NodeState.HARVESTED:
                node.SetStateEmpty();
                break;
            default:
                node.UpdateForState();
                break;
        }
    }
    
    private void ApplyRemotePlacements()
    {
        if (nodePlacements.Count == 0 || SceneContext.Instance?.GameModel == null)
            return;

        foreach (var placement in nodePlacements.Values)
            SetPlacement(placement);

        foreach (var model in AllModels())
        {
            if (model == null || nodePlacements.ContainsKey(model.nodeId))
                continue;

            SetPlacement(new ResourceNodePlacement { ID = model.nodeId, HasNode = false });
        }
    }

    private void SetPlacement(ResourceNodePlacement placement)
    {
        try
        {
            if (placement.HasNode && (ResourceNode.NodeState)placement.State == ResourceNode.NodeState.HARVESTING)
                RemotelyHarvested.Add(placement.ID);
            else
                RemotelyHarvested.Remove(placement.ID);

            var model = GetModel(placement.ID);
            if (model == null)
                return;

            var spawner = GetSpawner(placement.ID);

            HandlingPacket = true;
            try
            {
                if (!placement.HasNode)
                {
                    if (spawner?.HasAttachedNode == true)
                        spawner.DespawnNode();

                    model.resourceNodeDefinition = null!;
                    model.nodeState = ResourceNode.NodeState.NONE;
                    return;
                }

                var definitions = Il2CppSystem.Linq.Enumerable.ToArray(model.resourceNodeDefinitions);
                if (placement.DefinitionIndex < 0 || placement.DefinitionIndex >= definitions.Length)
                    return;

                var definition = definitions[placement.DefinitionIndex];

                var shouldUpdate = spawner != null
                    && (!spawner.HasAttachedNode
                        || model.resourceNodeDefinition != definition
                        || model.resourceNodeVariantIndex != placement.VariantIndex);

                if (shouldUpdate && spawner!.HasAttachedNode)
                {
                    var oldNode = spawner.AttachedNode;
                    spawner._attachedResourceNode = null!;
                    if (oldNode)
                        Object.Destroy(oldNode.gameObject);
                }

                SetModel(model, placement, definition);

                if (shouldUpdate)
                {
                    var prefabs = definition.NodePrefabs;
                    if (placement.VariantIndex >= 0 && placement.VariantIndex < prefabs.Length)
                        spawner!.CreateResourceNode(prefabs[placement.VariantIndex]);
                    else
                        spawner!.SpawnNode(definition);
                    
                    SetModel(model, placement, definition);
                    
                    StartCoroutine(ApplyState(placement.ID));
                }
                else
                {
                    var node = spawner?.AttachedNode;
                    if (node != null)
                        SetNodeState(node, (ResourceNode.NodeState)placement.State);
                }
            }
            finally
            {
                HandlingPacket = false;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"Failed to apply resource node placement {placement.ID}: {ex.Message}");
        }
    }

    private static void SetModel(ResourceNodeSpawnerModel model, ResourceNodePlacement placement, ResourceNodeDefinition definition)
    {
        model.resourceNodeDefinition = definition;
        model.resourceNodeVariantIndex = placement.VariantIndex;
        model.despawnAtWorldTime = placement.DespawnAtWorldTime;
        model.nodeState = (ResourceNode.NodeState)placement.State;

        var resources = new CppCollections.List<IdentifiableType>();
        foreach (var typeId in placement.ResourcesToSpawn)
        {
            if (ActorManager.ActorTypes.TryGetValue(typeId, out var identType) && identType != null)
                resources.Add(identType);
        }
        model.resourcesToSpawn = resources;
    }

    private IEnumerator ApplyState(string nodeId)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            yield return null;

            if (!nodePlacements.TryGetValue(nodeId, out var placement) || !placement.HasNode)
                yield break;

            var node = GetNode(nodeId);
            if (node == null || node._model == null)
                continue;

            var applied = false;

            HandlingPacket = true;
            try
            {
                SetNodeState(node, (ResourceNode.NodeState)placement.State);
                applied = true;
            }
            catch { /* ignored */ }
            finally
            {
                HandlingPacket = false;
            }

            if (applied)
                yield break;
        }
    }

    private void EnsureLoopStarted()
    {
        if (initialized)
            return;

        initialized = true;
        StartCoroutine(ZoneLoadingLoop());
    }

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            ApplyRemotePlacements();
        }
    }

    private static ResourceNodeSpawnerModel?[] AllModels()
        => Il2CppSystem.Linq.Enumerable
            .ToArray(GameState.AllResourceNodes()
                .Cast<Il2CppSystem.Collections.Generic.IEnumerable<ResourceNodeSpawnerModel>>());

    private static ResourceNodeSpawnerModel? GetModel(string nodeId)
    {
        foreach (var model in AllModels())
        {
            if (model?.nodeId == nodeId)
                return model;
        }

        return null;
    }

    private static ResourceNodeSpawner? GetSpawner(string nodeId)
    {
        foreach (var director in ResourceNodeDirector.AllResourceDirectors)
        {
            if (director == null || director.NodeSpawners == null) continue;
            foreach (var spawner in director.NodeSpawners)
            {
                if (spawner?._model?.nodeId == nodeId)
                    return spawner;
            }
        }

        return null;
    }

    private static ResourceNode? GetNode(string nodeId)
        => GetSpawner(nodeId)?.AttachedNode;
}
