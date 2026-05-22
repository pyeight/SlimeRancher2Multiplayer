using HarmonyLib;
using SR2MP.Packets.FX;
using SR2MP.Packets.GordoSlime;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.DoEat))]
internal static class OnGordoFed
{
    public static void Postfix(GordoEat __instance)
    {
        var packet = new GordoSlimeFeedPacket
        {
            ID = __instance.Id,
            NewFoodCount = __instance.GordoModel.GordoEatenCount,
            RequiredFoodCount = __instance.GordoModel.targetCount,
            GordoType = NetworkActorManager.GetPersistentID(__instance.GordoModel.identifiableType)
        };
        Main.SendToAllOrServer(packet);

        var soundPacket = new WorldFXPacket
        {
            Position = __instance.transform.position,
            FX = WorldFXType.GordoFoodEatenSound
        };
        Main.SendToAllOrServer(soundPacket);
    }
}