using HarmonyLib;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SplashOnTrigger), nameof(SplashOnTrigger.SpawnAndPlayFX))]
internal static class SplashOnTriggerDoFX
{
    public static void Postfix(SplashOnTrigger __instance, GameObject prefab, Collider collider)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;
        if (HandlingPacket) return;

        if (collider != null && collider.gameObject == SceneContext.Instance.Player)
        {
            Main.SendToAllOrServer(new PlayerFXPacket
            {
                FX = PlayerFXType.WaterSplash,
                Position = collider.transform.position,
                Player = LocalID
            });
        }
    }
}