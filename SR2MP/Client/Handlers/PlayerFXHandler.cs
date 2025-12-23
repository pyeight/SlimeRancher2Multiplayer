using Il2Cpp;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;
using UnityEngine;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public class PlayerFXHandler : BaseClientPacketHandler
{
    public PlayerFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerFXPacket>();
        
        SrLogger.LogMessage($"Received FX: {packet.FX}");

        if (!IsPlayerSoundDictionary[packet.FX])
        {
            SrLogger.LogMessage($"at {packet.Position}");
            var fxPrefab = fxManager.playerFXMap[packet.FX];

            handlingPacket = true;
            var fxObject = FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
        }
        else
        {
            SrLogger.LogMessage($"from {packet.Player}");
            var cue = fxManager.playerAudioCueMap[packet.FX];
            if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
            {
                fxManager.PlayTransientAudio(cue, playerObjects[packet.Player].transform.position);
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
}