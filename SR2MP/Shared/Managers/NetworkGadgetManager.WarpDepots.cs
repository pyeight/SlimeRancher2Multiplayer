using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkGadgetManager
{
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
}
