using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BaseSharedPacketHandler
{
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldFXPacket>();

        if (!IsWorldSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.WorldFXMap[packet.FX];

            handlingPacket = true;
            FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
        }
        else
        {
            var cue = fxManager.WorldAudioCueMap[packet.FX];

            RemoteFXManager.PlayTransientAudio(cue, packet.Position, WorldSoundVolumeDictionary[packet.FX]);
        }

        
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}