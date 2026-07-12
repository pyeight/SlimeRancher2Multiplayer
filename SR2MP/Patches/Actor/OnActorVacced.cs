using HarmonyLib;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.Capture))]
internal static class OnActorVacced
{
    public static void Postfix(Vacuumable __instance)
    {
        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor)
            return;

        networkActor.LocallyOwned = true;
        networkActor.CurrentOwnerId = LocalID;

        var packet = new ActorTransferPacket
        {
            ActorId = __instance._identifiable.GetActorId(),
            OwnerId = LocalID
        };

        Main.SendToAllOrServer(packet);
    }
}