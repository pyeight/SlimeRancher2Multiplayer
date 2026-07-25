using HarmonyLib;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(TimeDirector), nameof(TimeDirector.FastForwardTo))]
internal static class OnFastForward
{
    public static void Postfix(double fastForwardUntil)
    {
        NetworkGardenManager.RestoreAfterTimeSkip();

        if (HandlingPacket)
            return;

        var packet = new FastForwardPacket
        {
            Time = fastForwardUntil
        };

        Main.SendToAllOrServer(packet);
    }
}