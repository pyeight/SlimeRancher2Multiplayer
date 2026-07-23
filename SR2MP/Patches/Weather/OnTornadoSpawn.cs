using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather.Activity;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Weather;

[HarmonyPatch(typeof(SpawnTornadoActivity), nameof(SpawnTornadoActivity.Spawn))]
internal static class OnTornadoSpawn
{
    public static bool Prefix(SpawnTornadoActivity __instance)
    {
        NetworkTornadoManager.LastActivity = __instance;
        
        if (Main.Client.IsConnected && !Main.Server.IsRunning && !HandlingPacket)
            return false;

        return true;
    }

    public static void Postfix(Vector3 position, Quaternion rotation, GameObject __result)
    {
        try
        {
            if (!Main.Server.IsRunning || HandlingPacket || !__result)
                return;

            var id = NetworkTornadoManager.IncreaseId();
            NetworkTornadoManager.HostTornados[id] = __result;
            
            Main.Server.SendToAll(new TornadoSpawnPacket
            {
                TornadoId = id,
                Position = position,
                Rotation = rotation
            });
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"OnTornadoSpawn: {ex.Message}");
        }
    }
}
