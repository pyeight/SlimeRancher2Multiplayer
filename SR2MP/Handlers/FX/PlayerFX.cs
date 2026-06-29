using System.Net;
using SR2MP.Components.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
internal sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        try
        {
            if (!IsPlayerSoundDictionary[packet.FX])
            {
                var fxPrefab = FXManager.PlayerFXMap[packet.FX];
                FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            }
            else
            {
                var remotePlayer = PlayerObjects[packet.Player];
                PlayPlayerAudio(packet, remotePlayer);
            }
        }
        catch { /* Non-critical; typically triggered during scene transitions. */ }
        
        HandlingPacket = false;

        return true;
    }

    private static void PlayPlayerAudio(PlayerFXPacket packet, GameObject remotePlayer)
    {
        var cue = FXManager.PlayerAudioCueMap[packet.FX];

        if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
        {
            RemoteFXManager.PlayTransientAudio(
                cue,
                remotePlayer.transform.position,
                PlayerSoundVolumeDictionary[packet.FX]);
        }
        else
        {
            var playerAudio = remotePlayer.GetComponent<SECTR_PointSource>();
            playerAudio.Cue              = cue;
            playerAudio.Loop             = DoesPlayerSoundLoopDictionary[packet.FX];
            playerAudio.instance.Volume  = PlayerSoundVolumeDictionary[packet.FX];
            playerAudio.Play();
        }
    }
}