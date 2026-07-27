using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkGadgetManager
{
    private sealed class CachedTeleporterNode
    {
        public TeleporterNodeModel? Node;
        public SceneGroup? Scene;
    }

    private static readonly Dictionary<string, CachedTeleporterNode> KnownTeleporterNodes = new();
    
    private static readonly List<(string Source, string Destination)> CachedTeleporterLinks = new();
    
    private static readonly List<(string Source, string Destination, byte SceneGroup)> KnownTeleporterLinks = new();
    
    internal static void CacheTeleporterStates()
    {
        CachedTeleporterLinks.Clear();
        KnownTeleporterNodes.Clear();

        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var model = pair.Value;
                if (model == null) continue;

                string? sourceNodeId = null;

                var sourceNode = model.source?.TeleporterNodeModel;
                if (sourceNode != null)
                {
                    sourceNodeId = sourceNode.NodeId;
                    CacheTeleporterNode(sourceNode, null);
                }

                var destinations = model.destinations;
                if (destinations == null) continue;

                foreach (var destPair in destinations)
                {
                    var info = destPair.Value;
                    if (info?.TeleporterNodeModel == null) continue;

                    CacheTeleporterNode(info.TeleporterNodeModel, info.SceneGroup);

                    if (sourceNodeId != null)
                        CachedTeleporterLinks.Add((sourceNodeId, info.TeleporterNodeModel.NodeId));
                }
            }

            SrLogger.LogDebug($"CacheTeleporterStates: {CachedTeleporterLinks.Count} link(s), {KnownTeleporterNodes.Count} node(s).");
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"CacheTeleporterStates: {ex.Message}");
        }
    }

    private static void CacheTeleporterNode(TeleporterNodeModel node, SceneGroup? scene)
    {
        if (node.NodeId is not { Length: > 0 } nodeId) return;

        if (!KnownTeleporterNodes.TryGetValue(nodeId, out var cached))
        {
            cached = new CachedTeleporterNode();
            KnownTeleporterNodes[nodeId] = cached;
        }

        cached.Node = node;
        if (scene != null)
            cached.Scene = scene;
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

    private static bool TryResolveTeleporterNode(string nodeId, out TeleporterNodeModel? node, out SceneGroup? scene)
    {
        node = null;
        scene = null;

        try
        {
            foreach (var pair in GameState.teleporters)
            {
                var sourceNode = pair.Value?.source?.TeleporterNodeModel;
                if (sourceNode != null && sourceNode.NodeId == nodeId)
                {
                    node = sourceNode;
                    
                    if (KnownTeleporterNodes.TryGetValue(nodeId, out var known))
                        scene = known.Scene;

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
                    scene = destPair.Value?.SceneGroup;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"TryResolveTeleporterNode: {ex.Message}");
        }

        if (KnownTeleporterNodes.TryGetValue(nodeId, out var cached) && cached.Node != null)
        {
            node = cached.Node;
            scene = cached.Scene;
            return true;
        }

        return false;
    }

    private static TeleporterGadgetModel? FindTeleporterGadgetModel(string? nodeId)
    {
        if (nodeId is not { Length: > 0 })
            return null;

        try
        {
            foreach (var model in GameState.AllGadgets())
            {
                if (model.TryCast<TeleporterGadgetModel>(out var teleporter)
                    && teleporter.teleporterModel?.source?.TeleporterNodeModel?.NodeId == nodeId)
                    return teleporter;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"FindTeleporterGadgetModel ({nodeId}): {ex.Message}");
        }

        return null;
    }

    private static void RestoreTeleporterChargeup(TeleporterGadgetModel? gadget, double? chargeupTime)
    {
        if (gadget == null || chargeupTime is not { } value)
            return;

        if (Math.Abs(gadget.waitForChargeupTime - value) >= 0.0001)
            gadget.waitForChargeupTime = value;
        
        ReSetChargeup(gadget, value);
    }

    private static readonly float[] ChargeupReSetDelays = { 0f, 0.5f, 1f, 2f, 4f };

    internal static void ReSetChargeup(GadgetModel? model, double chargeupTime)
    {
        if (model != null && chargeupTime > 0)
            StartCoroutine(ReSetChargeupCoroutine(model, chargeupTime));
    }
    
    private static IEnumerator ReSetChargeupCoroutine(GadgetModel model, double chargeupTime)
    {
        var stable = 0;
        foreach (var delay in ChargeupReSetDelays)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;

            bool corrected;
            try
            {
                corrected = model.waitForChargeupTime > chargeupTime + 0.0001;
                if (corrected)
                    model.waitForChargeupTime = chargeupTime;
                
            }
            catch (Exception)
            {
                // Gadget was removed
                yield break;
            }

            stable = corrected ? 0 : stable + 1;
            if (stable >= 2)
                yield break;
        }
    }
    
    private static bool ForceApplyTeleporterLink(TeleporterModel sourceModel, TeleporterNodeModel destinationNode, SceneGroup sceneGroup)
    {
        var sourceGadget = FindTeleporterGadgetModel(sourceModel.source?.TeleporterNodeModel?.NodeId);
        var destinationGadget = FindTeleporterGadgetModel(destinationNode.NodeId);
        var sourceChargeup = sourceGadget?.waitForChargeupTime;
        var destinationChargeup = destinationGadget?.waitForChargeupTime;

        HandlingPacket = true;
        try
        {
            var destinations = sourceModel.destinations;
            if (destinations?.ContainsKey(destinationNode.NodeId) == true)
                destinations.Remove(destinationNode.NodeId);

            sourceModel.AddDestination(destinationNode, sceneGroup);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"ForceApplyTeleporterLink ({destinationNode.NodeId}): {ex.Message}");
            return false;
        }
        finally
        {
            HandlingPacket = false;
        }

        SceneContext.Instance?.TeleportNetwork?.OnLinkRegistered(sourceModel);

        RestoreTeleporterChargeup(sourceGadget, sourceChargeup);
        RestoreTeleporterChargeup(destinationGadget, destinationChargeup);
        return true;
    }

    private static bool TryApplyTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId, bool logRetry)
    {
        if (SceneContext.Instance == null)
            return false;

        var sourceModel = FindTeleporterSourceModel(sourceNodeId);
        if (sourceModel == null)
        {
            if (logRetry)
                SrLogger.LogDebug($"TeleporterLink: source {sourceNodeId} not registered yet, retrying.");
            return false;
        }

        if (!TryResolveTeleporterNode(destinationNodeId, out var destinationNode, out var cachedScene) || destinationNode == null)
        {
            if (logRetry)
                SrLogger.LogDebug($"TeleporterLink: destination {destinationNodeId} not registered yet, retrying.");
            return false;
        }

        SceneGroup? sceneGroup;
        try
        {
            sceneGroup = NetworkSceneManager.GetSceneGroup(sceneGroupId);
        }
        catch (Exception)
        {
            sceneGroup = null;
        }

        sceneGroup ??= cachedScene;
        if (sceneGroup == null)
            return false;

        if (!ForceApplyTeleporterLink(sourceModel, destinationNode, sceneGroup))
            return false;
        
        return true;
    }

    private static void RememberTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId)
    {
        for (var i = 0; i < KnownTeleporterLinks.Count; i++)
        {
            var link = KnownTeleporterLinks[i];
            if (link.Source != sourceNodeId || link.Destination != destinationNodeId)
                continue;

            KnownTeleporterLinks[i] = (sourceNodeId, destinationNodeId, sceneGroupId);
            return;
        }

        KnownTeleporterLinks.Add((sourceNodeId, destinationNodeId, sceneGroupId));
    }
    
    internal static IEnumerator ApplyTeleporterLink(string sourceNodeId, string destinationNodeId, byte sceneGroupId)
    {
        RememberTeleporterLink(sourceNodeId, destinationNodeId, sceneGroupId);

        var attempts = 0;
        while (attempts++ < 120)
        {
            if (!Main.Client.IsConnected && !Main.Server.IsRunning)
                yield break;

            bool applied;
            try
            {
                applied = TryApplyTeleporterLink(sourceNodeId, destinationNodeId, sceneGroupId, attempts == 1);
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

        SrLogger.LogWarning($"TeleporterLink: could not link {sourceNodeId} -> {destinationNodeId} (nodes never found)");
    }

    internal static IEnumerator RepairTeleporterLinks()
    {
        var waited = 0f;
        while ((SceneContext.Instance == null
                || !SceneContext.Instance.TeleportNetwork
                || SceneContext.Instance.player == null)
               && waited < 60f)
        {
            waited += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (SceneContext.Instance?.TeleportNetwork != true)
            yield break;
        
        yield return new WaitForSeconds(2f);

        var pending = new List<(string Source, string Destination)>(CachedTeleporterLinks);
        var repaired = 0;
        var attempts = 0;

        while (pending.Count > 0 && attempts++ < 120)
        {
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var (source, destination) = pending[i];
                var done = false;

                try
                {
                    var sourceModel = FindTeleporterSourceModel(source);
                    if (sourceModel != null
                        && TryResolveTeleporterNode(destination, out var node, out var scene)
                        && node != null && scene != null)
                    {
                        if (ForceApplyTeleporterLink(sourceModel, node, scene))
                            repaired++;
                        
                        done = true;
                    }
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"RepairTeleporterLinks: {ex.Message}");
                    done = true;
                }

                if (done)
                    pending.RemoveAt(i);
            }

            if (pending.Count > 0)
                yield return new WaitForSeconds(0.5f);
        }

        if (repaired > 0)
            SrLogger.LogDebug($"RepairTeleporterLinks: re-applied {repaired} teleporter link(s).");

        if (pending.Count > 0)
            SrLogger.LogWarning($"RepairTeleporterLinks: {pending.Count} link(s) could not be re-applied");
    }

    internal static List<InitialTeleporterLinksPacket.Link> GetAllTeleporterLinks()
    {
        var links = new List<InitialTeleporterLinksPacket.Link>();
        var seen = new HashSet<(string Source, string Destination)>();

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

                    if (!seen.Add((sourceNode.NodeId, info.TeleporterNodeModel.NodeId)))
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
        
        foreach (var link in KnownTeleporterLinks)
        {
            if (seen.Add((link.Source, link.Destination)))
            {
                links.Add(new InitialTeleporterLinksPacket.Link
                {
                    SourceNodeId = link.Source,
                    DestinationNodeId = link.Destination,
                    DestinationSceneGroup = link.SceneGroup
                });
            }
        }

        return links;
    }
}
