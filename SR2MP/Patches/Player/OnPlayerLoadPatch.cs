using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2E.Utils;
using SR2MP.Components.Player;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(SRCharacterController), nameof(SRCharacterController.Awake))]
public static class OnPlayerLoadPatch
{
    public static void Postfix(SRCharacterController __instance)
    {
        if (Main.Server.IsRunning())
        {
            var networkPlayer = __instance.AddComponent<NetworkPlayer>();
            networkPlayer.ID = "HOST";
            networkPlayer.IsLocal = true;
        }
        else if (Main.Client.IsConnected)
        {
            var networkPlayer = __instance.AddComponent<NetworkPlayer>();
            networkPlayer.ID = Main.Client.OwnPlayerId;
            networkPlayer.IsLocal = true;
        }
        else
        {
            Main.Client.OnConnected += (id) =>
            {
                var networkPlayer = __instance.AddComponent<NetworkPlayer>();
                networkPlayer.ID = id;
                networkPlayer.IsLocal = true;
            };

            Main.Server.OnServerStarted += () =>
            {
                var networkPlayer = __instance.AddComponent<NetworkPlayer>();
                networkPlayer.ID = "HOST";
                networkPlayer.IsLocal = true;
            };
        }
    }
}