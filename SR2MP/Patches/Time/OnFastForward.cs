using HarmonyLib;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(TimeDirector), nameof(TimeDirector.FastForwardTo))]
public static class OnFastForward
{
    public static void Postfix(double fastForwardUntil)
    {
        if (handlingPacket)
            return;

        var packet = new WorldTimePacket { Time = fastForwardUntil };

        if (Main.Server.IsRunning())
            Main.Server.SendToAll(packet);
        else if (Main.Client.IsConnected)
            Main.Client.SendPacket(packet);
    }
}