using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.AccessDoor;
using Il2CppMonomiPark.World;
using SR2MP.Packets.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(AccessDoorUIRoot), nameof(AccessDoorUIRoot.UnlockDoor))]
public static class OnAccessDoorUnlock
{
    public static void Postfix(AccessDoorUIRoot __instance)
    {
        var packet = new AccessDoorPacket
        {
            ID = __instance._door.Id,
            State = AccessDoor.State.OPEN
        };
        Main.SendToAllOrServer(packet);
    }
}