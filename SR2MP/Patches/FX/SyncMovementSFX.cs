using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2MP.Packets.FX;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SRCharacterController), nameof(SRCharacterController.Play), typeof(SECTR_AudioCue), typeof(bool))]
public static class SyncMovementSfx
{
    private static bool IsMovementSound(string cueName) // Jump, Run, Step and Land are specific values, do not change, they are the names used in the game
        => cueName.Contains("Jump") || cueName.Contains("Run") || cueName.Contains("Step") || cueName.Contains("Land");

    public static void Postfix(SRCharacterController __instance, SECTR_AudioCue cue)
    {
        // Do not change "Player", same reason as above
        if (!cue || !cue.name.Contains("Player") || !IsMovementSound(cue.name))
            return;

        if (!Main.IsMultiplayerActive)
            return;

        var packet = new MovementSoundPacket
        {
            CueName = cue.name,
            Position = __instance.Position,
        };

        if (Main.Server?.IsRunning() == true)
        {
            Main.Server.SendToAll(packet);
        }
        else if (Main.Client?.IsConnected == true)
        {
            Main.Client.SendPacket(packet);
        }
    }
}
