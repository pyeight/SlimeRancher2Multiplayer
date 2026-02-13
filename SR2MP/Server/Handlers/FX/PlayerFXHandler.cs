using System.Net;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers.FX;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BasePacketHandler<PlayerFXPacket>
{
    public PlayerFXHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(PlayerFXPacket packet, IPEndPoint clientEp)
    {
        if (!IsPlayerSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.PlayerFXMap[packet.FX];

            handlingPacket = true;
            FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
        }
        else
        {
            var cue = fxManager.PlayerAudioCueMap[packet.FX];
            if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
            {
                RemoteFXManager.PlayTransientAudio(cue, playerObjects[packet.Player].transform.position, PlayerSoundVolumeDictionary[packet.FX]);
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

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}