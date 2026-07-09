using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(TimeDirector), nameof(TimeDirector.FastForwardTo))]
internal static class OnFastForward
{
    public static void Postfix(double fastForwardUntil)
    {
        if (HandlingPacket)
            return;

        var packet = new FastForwardPacket
        {
            Time = fastForwardUntil
        };

        Main.SendToAllOrServer(packet);
    }
}