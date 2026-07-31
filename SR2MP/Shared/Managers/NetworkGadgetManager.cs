using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkGadgetManager
{
    internal static GadgetModel? CreateNetworkGadget(
        IdentifiableType type, ActorId actorId, SceneGroup scene, Vector3 position, Quaternion rotation,
        out GameObject gadget, Action<GadgetModel>? configure = null)
    {
        gadget = null!;

        HandlingPacket = true;
        var model = GameState.CreateGadgetModel(type.Cast<GadgetDefinition>(), actorId, scene, position, false);

        if (model != null)
        {
            model.eulerRotation = rotation.eulerAngles;
            configure?.Invoke(model);

            if (NetworkSceneManager.IsSceneGroupLoaded(scene))
                gadget = GadgetDirector.InstantiateGadgetFromModel(model);
        }
        HandlingPacket = false;

        if (gadget)
            gadget.transform.SetPositionAndRotation(position, rotation);

        return model;
    }

    internal static GadgetModel? GetLinkedGadget(GadgetModel model)
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

    // Every GadgetModel.LinkDestroyer resolves its own partner off the gadget registry. The proximity
    // fallback below picks an arbitrary gadget of the same type once a second pair exists, which for warp
    // depots means picking one up deletes someone else's depot, ammo and all.
    internal static GadgetModel? ResolveLinkedGadget(GadgetModel gadget)
        => gadget.TryCast<TeleporterGadgetModel>(out var teleporter) ? teleporter.GetLinkedGadget()
            : gadget.TryCast<WarpDepotModel>(out var depot) ? depot.GetLinkedGadget()
            : gadget.TryCast<LinkedCannonModel>(out var cannon) ? cannon.GetLinkedGadget()
            : GetLinkedGadget(gadget);

    internal static AmmoModel? GetAmmoFromGadget(GadgetModel model)
    {
        if (model.TryCast(out WarpDepotModel? warp))
            return warp.ammo;

        if (model.TryCast(out LinkedCannonModel? cannon))
            return cannon.Ammo;

        return null;
    }

    internal static void EnsureGadgetLinked(GadgetModel? model)
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
            var partnerModel = warpDepotModel.GetLinkedGadget()?.TryCast<WarpDepotModel>();
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
        // Fancy, I know
        a.linkedModel = b;
        b.linkedModel ??= a;
    }

    // Patching TeleporterModel.RemoveDestination to sync the unlink causes a memory access violation.
    internal static void BroadcastLinkedPairDestroy(ActorId actorId)
    {
        try
        {
            if (!GameState.TryGetIdentifiableModel(actorId, out var model)
                || model == null
                || !model.TryCast<GadgetModel>(out var gadget)
                || !gadget.DestroysLinkedPairOnRemoval())
                return;

            var partner = ResolveLinkedGadget(gadget);

            if (partner == null || partner.actorId.Value == actorId.Value)
                return;
            
            // Murderer!!
            SrLogger.LogDebug($"Gadget {actorId.Value} takes its pair {partner.actorId.Value} down with it");

            Main.SendToAllOrServer(new ActorDestroyPacket { ActorId = partner.actorId });
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to broadcast linked pair destroy for {actorId.Value}: {ex.Message}");
        }
    }

    internal static void BroadcastGadgetLink(GadgetModel? model)
    {
        if (HandlingPacket || model == null) return;
        if (model.TryCast<TeleporterGadgetModel>(out _)) return;

        var partner = ResolveLinkedGadget(model);
        if (partner == null) return;

        RecordGadgetLinkPair(model.actorId.Value, partner.actorId.Value);

        Main.SendToAllOrServer(new GadgetLinkingPacket
        {
            GadgetId = model.actorId.Value,
            PartnerId = partner.actorId.Value
        });
    }

    private static readonly Dictionary<long, long> PendingGadgetLinks = new();

    private static readonly Dictionary<long, long> GadgetLinkPartners = new();

    internal static bool TryGetLinkedGadgetId(long gadgetId, out long partnerId)
        => GadgetLinkPartners.TryGetValue(gadgetId, out partnerId);

    private static void RecordGadgetLinkPair(long gadgetId, long partnerId)
    {
        GadgetLinkPartners[gadgetId] = partnerId;
        GadgetLinkPartners[partnerId] = gadgetId;

        NetworkAmmoManager.OnGadgetLinkResolved(gadgetId, partnerId);
    }

    private static readonly Dictionary<long, double> CannonFireTimes = new();

    /// <summary>
    /// Records a cannon's next fire time and reports whether it moved since we last saw it.
    /// </summary>
    /// <remarks>
    /// A cannon first seen is adopted silently: broadcasting it would have every player announce their
    /// own timer the moment the gadget loads.
    /// </remarks>
    internal static bool TryRecordCannonFireTime(long gadgetId, double nextFireTime)
    {
        if (!CannonFireTimes.TryGetValue(gadgetId, out var previous))
        {
            CannonFireTimes[gadgetId] = nextFireTime;
            return false;
        }

        if (Math.Abs(previous - nextFireTime) < 0.0001)
            return false;

        CannonFireTimes[gadgetId] = nextFireTime;
        return true;
    }

    // Whoever fires first pushes everyone else's timer past the trigger, so the shot only happens once.
    internal static void ApplyCannonFireTime(long gadgetId, double nextFireTime)
    {
        // Recorded before it is applied, so the Update patch reads it back as its own value and stays quiet.
        CannonFireTimes[gadgetId] = nextFireTime;

        if (!TryGetGadgetModel(gadgetId, out var model) || model == null)
            return;

        var gameObject = model.GetGameObject();
        if (!gameObject)
            return;

        var cannon = gameObject.GetComponentInChildren<LinkedCannonInput>(true);
        if (!cannon)
            return;

        cannon._nextFireTime = nextFireTime;
    }

    // Both maps are keyed by actor id and nothing else prunes them, so a destroyed gadget would keep
    // handing NetworkAmmoManager a partner that no longer exists.
    internal static void ForgetGadgetLink(ActorId actorId)
    {
        var id = actorId.Value;

        CannonFireTimes.Remove(id);

        if (GadgetLinkPartners.Remove(id, out var partnerId)
            && GadgetLinkPartners.TryGetValue(partnerId, out var backReference)
            && backReference == id)
            GadgetLinkPartners.Remove(partnerId);

        PendingGadgetLinks.Remove(id);

        foreach (var (pendingGadgetId, pendingPartnerId) in PendingGadgetLinks.ToArray())
        {
            if (pendingPartnerId == id)
                PendingGadgetLinks.Remove(pendingGadgetId);
        }
    }

    internal static void ApplyGadgetLink(long gadgetId, long partnerId)
    {
        RecordGadgetLinkPair(gadgetId, partnerId);

        if (TryGetGadgetModel(gadgetId, out var gadgetModel) && TryGetGadgetModel(partnerId, out var partnerModel))
        {
            ApplyResolvedLink(gadgetModel!, partnerModel!);
            return;
        }

        PendingGadgetLinks[gadgetId] = partnerId;
    }

    internal static void CheckPendingGadgetLink(GadgetModel? model)
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

            var partner = ResolveLinkedGadget(gadget);
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

    private static void RelinkAllGadgets()
    {
        foreach (var model in GameState.AllGadgets())
        {
            if (model.TryCast<GadgetModel>(out var gadget))
                EnsureGadgetLinked(gadget);
        }
    }

    // Todo: change this if you hate your life
    private static readonly byte[] RelinkRetryFrameDelays = { 2, 10, 30, 60 };

    internal static IEnumerator RelinkGadgets()
    {
        foreach (var delay in RelinkRetryFrameDelays)
        {
            yield return new WaitFrames(delay);
            RelinkAllGadgets();
        }
    }
}
