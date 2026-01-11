using HarmonyLib;
using SR2MP.Packets.FX;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Gordo;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.DoEat))]
public static class OnGordoFed
{
    public static void Postfix(GordoEat __instance, GameObject obj)
    {
        var packet = new GordoFeedPacket
        {
            Type = (byte)PacketType.GordoFeed,
            ID = __instance.Id,
            NewFoodCount = __instance.GordoModel.GordoEatenCount,
            RequiredFoodCount = __instance.GordoModel.targetCount,
            GordoType = NetworkActorManager.GetPersistentID(__instance.GordoModel.identifiableType)
        };
        Main.SendToAllOrServer(packet);

        var soundPacket = new WorldFXPacket
        {
            Type = (byte)PacketType.WorldFX,
            Position = __instance.transform.position,
            FX = WorldFXType.GordoFoodEatenSound
        };
        Main.SendToAllOrServer(soundPacket);
    }
}