using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SRCharacterController), nameof(SRCharacterController.Play), typeof(SECTR_AudioCue), typeof(bool))]
public static class SyncMovementSFX
{
    private static bool IsMovementSound(string cueName) // Jump, Run, Step and Land are specific values, do not change, they are the names used in the game
        => cueName.Contains("Jump") || cueName.Contains("Run") || cueName.Contains("Step") || cueName.Contains("Land");
    //                                                          can not rename 'cue', breaks everything
    public static void Postfix(SRCharacterController __instance, SECTR_AudioCue cue, bool loop)
    {
        if (!cue)
            return;
        // Do not change "Player", same reason as above
        if (cue.name.Contains("Player") && IsMovementSound(cue.name))
        {
            var packet = new MovementSoundPacket()
            {
                Type = (byte)PacketType.MovementSound,
                CueName = cue.name,
                Position = __instance.Position,
            };
        
            if (Main.Server.IsRunning())
            {
                Main.Server.SendToAll(packet);
            }
            else if (Main.Client.IsConnected)
            {
                Main.Client.SendPacket(packet);
            }
        }
    }
}