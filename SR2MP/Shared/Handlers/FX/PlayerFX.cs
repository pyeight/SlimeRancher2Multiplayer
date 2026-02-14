using System.Net;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    public PlayerFXHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(PlayerFXPacket packet, IPEndPoint? _)
    {
        handlingPacket = true;

        try
        {
            if (!IsPlayerSoundDictionary[packet.FX])
            {
                var fxPrefab = fxManager.PlayerFXMap[packet.FX];
                FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            }
            else
            {
                var cue = fxManager.PlayerAudioCueMap[packet.FX];

                if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
                {
                    RemoteFXManager.PlayTransientAudio(cue, playerObjects[packet.Player].transform.position,
                        PlayerSoundVolumeDictionary[packet.FX]);
                }
                else
                {
                    var playerAudio = playerObjects[packet.Player].GetComponent<SECTR_PointSource>();
                    playerAudio.Cue = cue;
                    playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];
                    playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
                    playerAudio.Play();
                }
            }
        }
        catch { /* Errors here are typically non-serious related to scene loading */ }
        finally
        {
            handlingPacket = false;
        }

        return true;
    }
}