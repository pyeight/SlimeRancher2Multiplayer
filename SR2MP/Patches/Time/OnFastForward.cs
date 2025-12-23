using HarmonyLib;
using Il2Cpp;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Time;

[HarmonyPatch(typeof(TimeDirector), nameof(TimeDirector.FastForwardTo))]
public static class OnFastForward
{
    public static void Postfix(TimeDirector __instance, double fastForwardUntil)
    {
        if (handlingPacket)
            return;
        
        if (Main.Server.IsRunning())
        {
            var packet = new WorldTimePacket()
            {
                Type = (byte)PacketType.BroadcastFastForward,
                Time = fastForwardUntil
            };
            
            Main.Server.SendToAll(packet);
        }
        else if (Main.Client.IsConnected)
        {
            var packet = new WorldTimePacket()
            {
                Type = (byte)PacketType.FastForward,
                Time = fastForwardUntil
            };
            
            Main.Client.SendPacket(packet);
        }
    }
}