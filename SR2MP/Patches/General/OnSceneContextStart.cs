using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.World;

namespace SR2MP.Patches.General;

// AccessDoor.InitModel/SetModel cause memory crash
// Todo: find a better way
[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnSceneContextStart
{
    private static bool subscribed;

    public static void Postfix()
    {
        if (subscribed) return;
        subscribed = true;

        Main.Server.OnServerStarted += () => StartCoroutine(ApplicationLoop());
        Main.Client.OnConnected += _ => StartCoroutine(ApplicationLoop());
    }

    private static IEnumerator ApplicationLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            ApplyAccessDoors();
        }
    }

    private static void ApplyAccessDoors()
    {
        foreach (var door in Resources.FindObjectsOfTypeAll<AccessDoor>())
        {
            if (!door || string.IsNullOrEmpty(door._id)) continue;
            if (!GameState.doors.TryGetValue(door._id, out var model)) continue;
            if (door.CurrState == model.state) continue;

            HandlingPacket = true;
            door.CurrState = model.state;
            HandlingPacket = false;
        }
    }
}
