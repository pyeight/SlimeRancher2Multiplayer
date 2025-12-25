using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Utils;
using UnityEngine.SocialPlatforms;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.Capture))]
public static class OnActorVacced
{
    public static void Postfix(Vacuumable __instance, Joint toJoint)
    {
        var networkActor = __instance.GetComponent<NetworkActor>();
        if (!networkActor)
            return;

        networkActor.LocallyOwned = true;
        
        var packet = new ActorTransferPacket
        {
            Type = (byte)PacketType.ActorTransfer,
            ActorId = __instance._identifiable.GetActorId(),
            OwnerPlayer = LocalID,
        };
        
        Main.SendToAllOrServer(packet);
    }
}