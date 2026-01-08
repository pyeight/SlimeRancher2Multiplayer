using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using SR2MP.Packets.Pedia;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Pedia;

[HarmonyPatch(typeof(PediaDirector), nameof(PediaDirector.Unlock), typeof(PediaEntry), typeof(bool))]
public static class OnEntryUnlocked
{
    public static void Postfix(PediaEntry entry, bool showPopup = true)
    {
        if (handlingPacket) return;

        var packet = new PediaUnlockPacket()
        {
            Type = (byte)PacketType.PediaUnlock,
            ID = entry.PersistenceId,
            Popup = showPopup
        };

        Main.SendToAllOrServer(packet);
    }
}