using System.Net;
using Il2Cpp;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerFX)]
public class PlayerFXHandler : BasePacketHandler
{
    public PlayerFXHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerFXPacket>();

        if (!IsPlayerSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.playerFXMap[packet.FX];

            handlingPacket = true;
            FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
        }
        else
        {
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
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}