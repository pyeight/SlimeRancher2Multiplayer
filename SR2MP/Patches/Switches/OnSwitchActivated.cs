using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Switch;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Switches;

[HarmonyPatch]
public static class OnSwitchActivated
{
    [HarmonyPostfix, HarmonyPatch(typeof(WorldStatePrimarySwitch), nameof(WorldStatePrimarySwitch.SetStateForAll))]
    public static void SetPrimary(WorldStatePrimarySwitch __instance, SwitchHandler.State state, bool immediate)
    {
        SendPacket(state, __instance.SwitchDefinition.ID);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(WorldStateSecondarySwitch), nameof(WorldStateSecondarySwitch.SetState))]
    public static void SetSecondary(WorldStateSecondarySwitch __instance, SwitchHandler.State state, bool immediate)
    {
        SendPacket(state, __instance._primary.SwitchDefinition.ID);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(WorldStateInvisibleSwitch), nameof(WorldStateInvisibleSwitch.SetStateForAll))]
    public static void SetInvisible(WorldStateInvisibleSwitch __instance, SwitchHandler.State state, bool immediate)
    {
        SendPacket(state, __instance.SwitchDefinition.ID);
    }

    private static void SendPacket(SwitchHandler.State state, string id)
    {
        if (handlingPacket) return;

        Main.SendToAllOrServer(new WorldSwitchPacket()
        {
            Type = (byte)PacketType.SwitchActivate,
            ID = id,
            State = state,
        });
    }
}