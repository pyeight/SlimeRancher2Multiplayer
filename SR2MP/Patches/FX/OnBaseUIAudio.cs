using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppMonomiPark.SlimeRancher.UI.Plot;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(BaseUI))]
public static class OnPlayUIAudio
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(BaseUI.Play))]
    public static bool OnPlay(BaseUI __instance, SECTR_AudioCue cue)
    {
        if (!SceneContext.Instance)
            return true;
        if (!fxManager.TryGetFXType(cue, out WorldFXType fxType))
            return true;
        
        SendPacket(fxType, SceneContext.Instance.player.transform.position);
        
        fxManager.PlayTransientAudio(fxManager.worldAudioCueMap[fxType], SceneContext.Instance.player.transform.position);

        return false;
    }
    
    static void SendPacket(WorldFXType fxType, Vector3 position)
    {
        var packet = new WorldFXPacket
        {
            Type = (byte)PacketType.WorldFX,
            FX = fxType,
            Position = position
        };
        
        Main.SendToAllOrServer(packet);
    }
}