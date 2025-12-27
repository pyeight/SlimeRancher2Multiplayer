using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(VacuumItem), nameof(VacuumItem.PlayTransientAudio))]
public static class OnPlayVacAudio
{
    // Note: You CAN rename cue by using [HarmonyArgument(0)] SECTR_AudioCue youNewName - Az
    public static void Postfix(SECTR_AudioCue cue)
    {
        if (!fxManager.TryGetFXType(cue, out PlayerFXType fxType))
        {
            return;
        }

        var packet = new PlayerFXPacket()
        {
            Type = (byte)PacketType.PlayerFX,
            FX = fxType,
            Player = LocalID
        };

        Main.SendToAllOrServer(packet);
    }
}