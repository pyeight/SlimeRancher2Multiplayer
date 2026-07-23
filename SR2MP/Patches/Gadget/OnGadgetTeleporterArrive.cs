using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World.Teleportation;
using SR2MP.Packets.Gadget;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(TeleporterNode), nameof(TeleporterNode.OnArrive))]
internal static class OnGadgetTeleporterArrive
{
    public static void Postfix(TeleporterNode __instance)
    {
        if (HandlingPacket)
            return;

        if (__instance.TryCast<GadgetTeleporterNode>() == null)
            return;

        Main.SendToAllOrServer(new GadgetTeleporterActivatePacket
        {
            ObjectPath = __instance.gameObject.GetGameObjectPath(),
            IsArrival = true
        });
    }
}
