using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkGadgetManager
{
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

    internal static AmmoModel? GetAmmoFromGadget(GadgetModel model)
    {
        if (model.TryCast(out WarpDepotModel? warp))
            return warp.ammo;

        return null!;
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
        // Fancy, I know
        a.linkedModel = b;
        b.linkedModel ??= a;
    }

    internal static void BroadcastGadgetLink(GadgetModel? model)
    {
        if (HandlingPacket || model == null) return;
        if (model.TryCast<TeleporterGadgetModel>(out _)) return;

        var partner = GetLinkedGadget(model);
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
