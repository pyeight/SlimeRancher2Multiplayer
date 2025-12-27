using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(VacuumItem), nameof(VacuumItem.PlayTransientAudio))]
public static class OnPlayVacAudio
{
    // Note: You CAN rename cue by using [HarmonyArgument(0)] SECTR_AudioCue youNewName - Az
    public static void Postfix(SECTR_AudioCue cue)
    {
        SendPacket(cue);
    }
    static void SendPacket(SECTR_AudioCue cue)
    {
        // If the audio cue isn't in the player dictionary
        if (!fxManager.TryGetFXType(cue, out var fxType))
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