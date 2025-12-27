using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public sealed class PlayerFXHandler : BaseClientPacketHandler
{
    public PlayerFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerFXPacket>();

        if (!IsPlayerSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.playerFXMap[packet.FX];

            handlingPacket = true;
            FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
            return;
        }

        var cue = fxManager.playerAudioCueMap[packet.FX];

        if (ShouldPlayerSoundBeTransientDictionary[packet.FX])
        {
            RemoteFXManager.PlayTransientAudio(cue, playerObjects[packet.Player].transform.position);
        }
        else
        {
            var playerAudio = playerObjects[packet.Player].GetComponent<SECTR_PointSource>();

            handlingPacket = true;
            playerAudio.Cue = cue;
            playerAudio.Loop = DoesPlayerSoundLoopDictionary[packet.FX];
            playerAudio.instance.Volume = PlayerSoundVolumeDictionary[packet.FX];
            playerAudio.Play();
            handlingPacket = false;
        }
    }
}