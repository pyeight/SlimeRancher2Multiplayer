using HarmonyLib;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo;

[HarmonyPatch(typeof(SiloStorage), nameof(SiloStorage.MaybeAddAsResource),
    typeof(IdentifiableType), typeof(int), typeof(int), typeof(bool))]
internal static class OnSiloStorageAddResource
{
    // For Drones
    // MaybeAddAsResource routes through MaybeAddToXSlot, so we need this to prevent duplication
    internal static bool isDroneResource;

    public static void Prefix() => isDroneResource = true;

    public static void Postfix(SiloStorage __instance, bool __result, IdentifiableType id, int slotIdx, int count)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

        if (!__result)
            return;

        var packet = new AmmoAddToSlotPacket
        {
            Identifiable = NetworkActorManager.GetPersistentID(id),
            SlotIndex = slotIdx,
            Count = count,
            ID = __instance.GetRelevantAmmo()?.GetPlotID()
        };

        if (packet.ID == null) return;

        Main.SendToAllOrServer(packet);
    }
    
    public static Exception? Finalizer(Exception? __exception)
    {
        isDroneResource = false;
        return __exception;
    }
}
