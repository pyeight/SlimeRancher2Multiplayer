using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using UnityEngine;
using System.Linq;
using SR2MP.Components.World;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(GadgetDirector), nameof(GadgetDirector.InstantiateGadget))]
public static class GadgetDirectorPatch
{
    public static void Postfix(GameObject original, Vector3 position, Quaternion rotation, GameObject __result)
    {
        if (GlobalVariables.handlingPacket) return;
        if (__result == null) return; 

        var netGadget = __result.GetComponent<NetworkGadget>();
        if (netGadget == null) return;

        var packet = new GadgetPacket
        {
            Type = (byte)PacketType.Gadget,
            GadgetId = netGadget.GadgetId,
            GadgetTypeId = GlobalVariables.actorManager.GetPersistentID(original.GetComponent<Identifiable>().identType),
            Position = position,
            Rotation = rotation,
            IsRemoval = false
        };

        Main.SendToAllOrServer(packet);
    }
}
