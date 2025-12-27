using HarmonyLib;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.FX;

[HarmonyPatch(typeof(SECTR_PointSource), nameof(SECTR_PointSource.Play))]
public static class OnPointSourcePlay
{
    public static void Postfix(SECTR_PointSource __instance)
    {
        if (handlingPacket) return;

        SendPacketWorld(__instance.Cue, __instance.transform.position);

        // Add more SendPacket____()'s when you make new FXTypes.
    }
    static void SendPacketWorld(SECTR_AudioCue cue, Vector3 position)
    {
        if (!fxManager.TryGetFXType(cue, out WorldFXType fxType))
        {
            return;
        }

        var packet = new WorldFXPacket
        {
            Type = (byte)PacketType.WorldFX,
            FX = fxType,
            Position = position
        };

        Main.SendToAllOrServer(packet);
    }
}