using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Refinery;

[HarmonyPatch(typeof(GadgetsModel), nameof(GadgetsModel.SetCount))]
internal static class RefineryUpdate
{
    public static void Postfix(GadgetsModel __instance, IdentifiableType type, int newCount)
    {
        if (HandlingPacket)
            return;

        var packet = new RefineryUpdatePacket
        {
            ItemCount = (ushort)newCount,
            ItemID = (ushort)GameContext.Instance.AutoSaveDirector._saveReferenceTranslation
                ._identifiableTypeToPersistenceId.GetPersistenceId(type)
        };

        Main.SendToAllOrServer(packet);
    }
}