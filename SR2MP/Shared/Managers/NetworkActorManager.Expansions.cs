using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.Slime;
using Il2CppMonomiPark.SlimeRancher.VFX;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public void RegisterSpawnOverNetwork(GameObject actor)
    {
        if (!actor) return;
        if (actor.GetComponent<NetworkActor>() != null) return;

        var identifiableActor = actor.GetComponent<IdentifiableActor>();
        if (!identifiableActor) return;

        var model = identifiableActor._model;
        if (model == null) return;
        if (NetworkDroneManager.IsDroneModel(model)) return;

        var networkActor = actor.AddComponent<NetworkActor>();
        networkActor.LocallyOwned = true;
        networkActor.CurrentOwnerId = LocalID;

        Actors[model.actorId.Value] = model;

        var packet = new ActorSpawnPacket
        {
            ActorId = identifiableActor.GetActorId(),
            ActorType = GetPersistentID(actor.GetComponent<Identifiable>().identType),
            SceneGroup = (byte)NetworkSceneManager.GetPersistentID(model.sceneGroup),
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
            SpawnType = (byte)ActorSpawnType.Actor,
            OwnerId = LocalID
        };

        var slimeModel = model.TryCast<SlimeModel>();
        if (slimeModel != null)
        {
            packet.SpawnType = (byte)ActorSpawnType.Slime;
            packet.Emotions = slimeModel.Emotions;
            packet.Sleeping = slimeModel.isSleeping;
            packet.FirstAppearance = slimeModel.firstAppearanceSaveSet;
            packet.SecondAppearance = slimeModel.secondAppearanceSaveSet;
            packet.Radiancy = (byte)ActorAppearanceType.Default;
        }

        Main.SendToAllOrServer(packet);
    }
    
    private static GadgetModel? GetLinkedGadget(GadgetModel model)
    {
        if (model?.ident == null)
            return null;

        var definition = model.ident.Cast<GadgetDefinition>();
        if (!definition.BuyInPairs && !definition.LinkedDefinition && Math.Abs(definition.LinkedGadgetRange) <= 0.0001f)
            return null;

        var candidates = GameState.identifiables._entries
            .Where(x => x.value != null && x.value != model && x.value.ident == model.ident)
            .Select(x => x.value.Cast<GadgetModel>())
            .ToList();

        if (candidates.Count <= 1)
            return candidates.FirstOrDefault();

        var range = definition.LinkedGadgetRange;
        if (Math.Abs(range) <= 0.0001f)
            return candidates.FirstOrDefault();

        var position = model.GetPos();
        return candidates
            .Where(c => Vector3.Distance(c.GetPos(), position) <= range)
            .OrderBy(c => Vector3.Distance(c.GetPos(), position))
            .FirstOrDefault();
    }
    
    private static AmmoModel? GetAmmoFromGadget(GadgetModel model)
    {
        if (model.TryCast(out WarpDepotModel? warp))
            return warp.ammo;
        
        return null!;
    }

    private static void EnsureGadgetLinked(GadgetModel? model)
    {
        if (model == null)
            return;

        if (model.TryCast(out WarpDepotModel? warpDepotModel))
            EnsureWarpDepotLinked(warpDepotModel);
    }

    private static void EnsureWarpDepotLinked(WarpDepotModel? warpDepotModel)
    {
        if (warpDepotModel == null || warpDepotModel.linkedModel != null)
            return;

        try
        {
            var partnerModel = GetLinkedGadget(warpDepotModel)?.TryCast<WarpDepotModel>();
            if (partnerModel != null)
                LinkWarpDepotModels(warpDepotModel, partnerModel);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to link warp depot {warpDepotModel.actorId.Value}: {ex.Message}");
        }
    }

    private static void LinkWarpDepotModels(WarpDepotModel a, WarpDepotModel b)
    {
        // Yes, this works
        a.linkedModel = b;
        b.linkedModel ??= a;
    }
    
    internal static void BroadcastGadgetLink(GadgetModel? model)
    {
        if (HandlingPacket || model == null) return;
        if (model.TryCast<TeleporterGadgetModel>(out _)) return;

        var partner = GetLinkedGadget(model);
        if (partner == null) return;

        Main.SendToAllOrServer(new GadgetLinkingPacket
        {
            GadgetId = model.actorId.Value,
            PartnerId = partner.actorId.Value
        });
    }

    private static readonly Dictionary<long, long> PendingGadgetLinks = new();

    internal static void ApplyGadgetLink(long gadgetId, long partnerId)
    {
        if (TryGetGadgetModel(gadgetId, out var gadgetModel) && TryGetGadgetModel(partnerId, out var partnerModel))
        {
            ApplyResolvedLink(gadgetModel!, partnerModel!);
            return;
        }

        PendingGadgetLinks[gadgetId] = partnerId;
    }
    
    private static void CheckPendingGadgetLink(GadgetModel? model)
    {
        if (model == null || PendingGadgetLinks.Count == 0) return;

        var id = model.actorId.Value;

        foreach (var (gadgetId, partnerId) in PendingGadgetLinks.ToArray())
        {
            if (gadgetId != id && partnerId != id)
                continue;

            if (!TryGetGadgetModel(gadgetId, out var gadgetModel) || !TryGetGadgetModel(partnerId, out var partnerModel))
                continue;

            PendingGadgetLinks.Remove(gadgetId);
            ApplyResolvedLink(gadgetModel!, partnerModel!);
        }
    }

    private static bool TryGetGadgetModel(long id, out GadgetModel? model)
    {
        model = null;
        if (!GameState.identifiables.TryGetValue(new ActorId(id), out var identModel))
            return false;

        model = identModel?.TryCast<GadgetModel>();
        return model != null;
    }

    private static void ApplyResolvedLink(GadgetModel gadgetModel, GadgetModel partnerModel)
    {
        if (gadgetModel.TryCast(out WarpDepotModel? warpA) && partnerModel.TryCast(out WarpDepotModel? warpB))
            LinkWarpDepotModels(warpA, warpB);
    }

    internal static List<InitialGadgetLinksPacket.Link> GetAllGadgetLinks()
    {
        var links = new List<InitialGadgetLinksPacket.Link>();
        var seen = new HashSet<long>();

        foreach (var model in GameState.AllGadgets())
        {
            if (!model.TryCast<GadgetModel>(out var gadget) || seen.Contains(gadget.actorId.Value))
                continue;

            if (gadget.TryCast<TeleporterGadgetModel>(out _))
                continue;

            var partner = GetLinkedGadget(gadget);
            if (partner == null)
                continue;

            seen.Add(gadget.actorId.Value);
            seen.Add(partner.actorId.Value);

            links.Add(new InitialGadgetLinksPacket.Link
            {
                GadgetId = gadget.actorId.Value,
                PartnerId = partner.actorId.Value
            });
        }

        return links;
    }
    
    private sealed class CachedTeleporterNode
    {
        public TeleporterNodeModel? Node;
        public SceneGroup? Scene;
    }

    private static readonly Dictionary<string, CachedTeleporterNode> CachedTeleporterNodes = new();

    internal static void CacheTeleporterNode(TeleporterNodeModel? node, SceneGroup? scene = null)
    {
        if (node?.NodeId is not { Length: > 0 } nodeId) return;

        if (!CachedTeleporterNodes.TryGetValue(nodeId, out var cached))
        {
            cached = new CachedTeleporterNode();
            CachedTeleporterNodes[nodeId] = cached;
        }

        cached.Node = node;
        if (scene != null)
            cached.Scene = scene;
    }
    
    internal static void CacheTeleporterState()
    {
        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var model = pair.Value;
                if (model == null) continue;

                CacheTeleporterNode(model.source?.TeleporterNodeModel);

                var destinations = model.destinations;
                if (destinations == null) continue;

                foreach (var destPair in destinations)
                {
                    var info = destPair.Value;
                    if (info?.TeleporterNodeModel == null) continue;
                    CacheTeleporterNode(info.TeleporterNodeModel, info.SceneGroup);
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"CacheTeleporterState: {ex.Message}");
        }
    }

    private static TeleporterModel? FindTeleporterSourceModel(string sourceNodeId)
    {
        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var sourceNode = pair.Value?.source?.TeleporterNodeModel;
                if (sourceNode != null && sourceNode.NodeId == sourceNodeId)
                    return pair.Value;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"FindTeleporterSourceModel: {ex.Message}");
        }

        return null;
    }
    
    private static bool TryResolveTeleporterNode(string nodeId, out TeleporterNodeModel? node, out SceneGroup? cachedScene)
    {
        cachedScene = null;

        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var sourceNode = pair.Value?.source?.TeleporterNodeModel;
                if (sourceNode != null && sourceNode.NodeId == nodeId)
                {
                    node = sourceNode;
                    CacheTeleporterNode(node);
                    return true;
                }

                var destinations = pair.Value?.destinations;
                if (destinations == null)
                    continue;

                foreach (var destPair in destinations)
                {
                    var destNode = destPair.Value?.TeleporterNodeModel;
                    if (destNode == null || destNode.NodeId != nodeId)
                        continue;

                    node = destNode;
                    cachedScene = destPair.Value?.SceneGroup;
                    CacheTeleporterNode(node, cachedScene);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"TryResolveTeleporterNode: {ex.Message}");
        }

        if (CachedTeleporterNodes.TryGetValue(nodeId, out var cached) && cached.Node != null)
        {
            node = cached.Node;
            cachedScene ??= cached.Scene;
            return true;
        }

        node = null;
        return false;
    }

    private static bool TryApplyTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId)
    {
        var sourceModel = FindTeleporterSourceModel(sourceNodeId);
        if (sourceModel == null)
        {
            SrLogger.LogDebug($"TeleporterLink: source node {sourceNodeId} not registered yet, will retry.");
            return false;
        }

        if (!TryResolveTeleporterNode(destinationNodeId, out var destinationNode, out var cachedScene) || destinationNode == null)
        {
            SrLogger.LogDebug($"TeleporterLink: destination node {destinationNodeId} not registered yet, will retry.");
            return false;
        }
        
        SceneGroup? sceneGroup;
        try
        {
            sceneGroup = NetworkSceneManager.GetSceneGroup(sceneGroupId);
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"TeleporterLink: scene group {sceneGroupId} not resolvable: {ex.Message}, trying cached scene.");
            sceneGroup = null;
        }

        sceneGroup ??= cachedScene;

        if (sceneGroup == null)
            return false;

        HandlingPacket = true;
        try
        {
            // Remove the old entry (after a reconnect)
            var destinations = sourceModel.destinations;
            if (destinations?.ContainsKey(destinationNode.NodeId) == true)
                destinations.Remove(destinationNode.NodeId);

            sourceModel.AddDestination(destinationNode, sceneGroup);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"TeleporterLink: failed to apply {sourceNodeId} -> {destinationNodeId}: {ex.Message}");
            return false;
        }
        finally
        {
            HandlingPacket = false;
        }

        SceneContext.Instance?.TeleportNetwork?.OnLinkRegistered(sourceModel);
        return true;
    }

    private static readonly List<(string Source, string Destination, byte SceneGroup)> CachedTeleporterLinks = new();

    private static void CacheTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId)
    {
        for (var i = 0; i < CachedTeleporterLinks.Count; i++)
        {
            var link = CachedTeleporterLinks[i];
            if (link.Source != sourceNodeId || link.Destination != destinationNodeId)
                continue;

            CachedTeleporterLinks[i] = (sourceNodeId, destinationNodeId, sceneGroupId);
            return;
        }

        CachedTeleporterLinks.Add((sourceNodeId, destinationNodeId, sceneGroupId));
    }

    internal static IEnumerator ApplyTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId)
    {
        CacheTeleporterLink(sourceNodeId, destinationNodeId, sceneGroupId);

        var attempts = 0;
        while (attempts++ < 120)
        {
            if (!Main.Client.IsConnected && !Main.Server.IsRunning)
                yield break;

            bool applied;
            try
            {
                applied = TryApplyTeleporterLink(sourceNodeId, destinationNodeId, sceneGroupId);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"TeleporterLink: {ex.Message}");
                yield break;
            }

            if (applied)
                yield break;

            yield return new WaitForSeconds(0.5f);
        }

        SrLogger.LogWarning($"TeleporterLink: gave up linking {sourceNodeId} -> {destinationNodeId} (nodes never appeared).");
    }
    
    internal static IEnumerator RelinkTeleporters()
    {
        foreach (var delay in RelinkRetryFrameDelays)
        {
            yield return new WaitFrames(delay);

            foreach (var link in CachedTeleporterLinks)
            {
                try
                {
                    TryApplyTeleporterLink(link.Source, link.Destination, link.SceneGroup);
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"RelinkTeleporters: {ex.Message}");
                }
            }
        }
    }

    internal static List<InitialTeleporterLinksPacket.Link> GetAllTeleporterLinks()
    {
        var links = new List<InitialTeleporterLinksPacket.Link>();

        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var sourceNode = pair.Value?.source?.TeleporterNodeModel;
                var destinations = pair.Value?.destinations;
                if (sourceNode == null || destinations == null)
                    continue;

                foreach (var destPair in destinations)
                {
                    var info = destPair.Value;
                    if (info?.TeleporterNodeModel == null)
                        continue;

                    links.Add(new InitialTeleporterLinksPacket.Link
                    {
                        SourceNodeId = sourceNode.NodeId,
                        DestinationNodeId = info.TeleporterNodeModel.NodeId,
                        DestinationSceneGroup = (byte)NetworkSceneManager.GetPersistentID(info.SceneGroup)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"GetAllTeleporterLinks: {ex.Message}");
        }

        return links;
    }

    private static void RelinkAllGadgets()
    {
        foreach (var model in GameState.AllGadgets())
        {
            if (model.TryCast<GadgetModel>(out var gadget))
                EnsureGadgetLinked(gadget);
        }
    }

    private static readonly byte[] RelinkRetryFrameDelays = { 2, 10, 30, 60 };

    internal static IEnumerator RelinkGadgets()
    {
        foreach (var delay in RelinkRetryFrameDelays)
        {
            yield return new WaitFrames(delay);
            RelinkAllGadgets();
        }
    }

    internal void SendActorTypeRegistry(IPEndPoint clientEndPoint)
    {
        if (!Main.Server.IsRunning) return;

        var packet = new ActorTypeRegistryPacket
        {
            Registry = new Dictionary<int, string>(ActorTypes.Count)
        };

        foreach (var (persistentId, type) in ActorTypes)
        {
            if (type == null) continue;
            packet.Registry[persistentId] = type.ReferenceId;
        }

        Main.Server.SendToClient(packet, clientEndPoint);
    }
    
    internal static bool ApplyRadiancy(SlimeModel slime, ActorAppearanceType radiancy = ActorAppearanceType.Default)
    {
        if (slime == null) return false;
        
        var gameObj = slime.GetGameObject();
        if (!gameObj) return false;
        
        var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
        if (!applicator) return false;
        
        var def = gameObj.GetComponent<Identifiable>().identType.TryCast<SlimeDefinition>();
        if (!def) return false;
        
        if (radiancy == ActorAppearanceType.Default && slime.IsRadiant)
        {
            if (def!.RadiantBase && def.RadiantBase.AppearType == SlimeAppearance.AppearanceType.RADIANT_BASE)
                radiancy = ActorAppearanceType.BaseRadiant;
            else if (def.RadiantLargo0 &&
                     def.RadiantLargo0.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_0)
                radiancy = ActorAppearanceType.LargoRadiant0;
            else if (def.RadiantLargo1 &&
                     def.RadiantLargo1.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_1)
                radiancy = ActorAppearanceType.LargoRadiant1;
        }
        
        var newAppearance = radiancy switch
        {
            ActorAppearanceType.BaseRadiant => def!.RadiantBase,
            ActorAppearanceType.LargoRadiant0 => def!.RadiantLargo0,
            ActorAppearanceType.LargoRadiant1 => def!.RadiantLargo1,
            _ => applicator.Appearance
        };
        
        if (!newAppearance) return false;
        
        var slimeRadiant = gameObj.GetComponent<SlimeRadiant>();
        if (slimeRadiant)
        {
            slimeRadiant.SetRadiant();
            slimeRadiant.SetRadiantAppearance();
        }
        
        slime.GetAmmoMetadata().Radiant = true;
        applicator.Appearance = newAppearance;
        applicator.ApplyAppearance();
        applicator.HandleChosenAppearanceChanged(def, newAppearance);
        
        return true;
    }
    
    private static IEnumerator EnsureSlimeAppearance(SlimeModel slime)
    {
        yield return new WaitFrames(2);

        if (slime == null) yield break;

        var gameObj = slime.GetGameObject();
        if (!gameObj) yield break;

        var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
        if (!applicator || applicator.Appearance) yield break;

        if (slime.IsRadiant && ApplyRadiancy(slime)) yield break;

        var def = gameObj.GetComponent<Identifiable>()?.identType.TryCast<SlimeDefinition>();
        if (!def) yield break;

        var appearance = def!.IsLargo
            ? def.GetLargoAppearance(slime.firstAppearanceSaveSet, slime.secondAppearanceSaveSet)
            : def.GetAppearanceForSet(slime.firstAppearanceSaveSet);

        if (!appearance)
            appearance = SceneContext.Instance?.SlimeAppearanceDirector?.GetChosenSlimeAppearance(def);

        if (!appearance)
            appearance = def.GetDefaultAppearance();

        if (!appearance) yield break;

        applicator.Appearance = appearance;
        applicator.ApplyAppearance();
    }

    internal static IEnumerator ApplySprinkleMaterial(GameObject gameObj, SprinkleMaterialType material)
    {
        yield return new WaitFrames(2);
        
        if (!gameObj) yield break;
        
        var sprinkle = gameObj.GetComponent<RandomMaterial>();
        if (!sprinkle) yield break;

        sprinkle.SetMaterial((int)material);
    }

    internal static void ApplyOwnership(ActorTransferPacket packet)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;
        
        component.CurrentOwnerId = packet.OwnerId;

        var locallyOwned = packet.OwnerId == LocalID;

        if (!locallyOwned)
        {
            try
            {
                var player = SceneContext.Instance.Player;
                var gameObject = actor.GetGameObject();

                if (player && gameObject)
                {
                    var vacItem = player.GetComponent<PlayerItemController>()._vacuumItem;

                    if (vacItem && vacItem._held == gameObject)
                    {
                        vacItem.LockJoint.connectedBody = null;
                        vacItem._held = null;
                        vacItem.SetHeldRad(0f);
                        vacItem._vacMode = VacuumItem.VacMode.NONE;
                        gameObject.GetComponent<Vacuumable>().Release();
                    }
                }
            }
            catch (Exception exception)
            {
                SrLogger.LogDebug($"Failed to release actor from vacuum on ownership change: {exception.Message}");
            }
        }

        component.LocallyOwned = locallyOwned;
    }
    
    internal static void RemoveExistingGadgetModel(ActorId actorId)
    {
        if (actorId.Value == 0) return;

        try
        {
            foreach (var gadget in GameState.AllGadgets().ToArray())
            {
                if (gadget == null || gadget.actorId.Value != actorId.Value)
                    continue;

                var gameObject = gadget.GetGameObject();

                HandlingPacket = true;
                if (gameObject)
                    Destroyer.DestroyGadget(gameObject, "SR2MP.RemoveExistingGadgetModel");
                else
                    GameState.DestroyGadgetModel(gadget);
                HandlingPacket = false;
                
                var mapDirector = SceneContext.Instance?.MapDirector;
                if (mapDirector != null)
                    mapDirector.DeregisterMarker(gadget);
            }
        }
        catch (Exception ex)
        {
            HandlingPacket = false;
            SrLogger.LogWarning($"Failed to remove existing gadget model for {actorId.Value}: {ex.Message}");
        }
    }
}