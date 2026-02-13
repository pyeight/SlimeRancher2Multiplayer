using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(BaseUI), nameof(BaseUI.Play))]
public static class OnPlayUIAudio
{
    [HarmonyPrefix]
    public static bool OnPlay(SECTR_AudioCue cue)
    {
        if (!SceneContext.Instance)
            return true;
        if (!fxManager.TryGetFXType(cue, out WorldFXType fxType))
            return true;

        SendPacket(fxType, SceneContext.Instance.player.transform.position);

        RemoteFXManager.PlayTransientAudio(fxManager.WorldAudioCueMap[fxType], SceneContext.Instance.player.transform.position, 1f);

        return false;
    }

    private static void SendPacket(WorldFXType fxType, Vector3 position)
    {
        var packet = new WorldFXPacket
        {
            FX = fxType,
            Position = position
        };

        Main.SendToAllOrServer(packet);
    }
}