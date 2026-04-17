using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.FX;

[PacketHandler((byte)PacketType.WorldFX)]
internal sealed class WorldFXHandler : BasePacketHandler<WorldFXPacket>
{
    protected override bool Handle(WorldFXPacket packet, IPEndPoint? _)
    {
        if (!IsWorldSoundDictionary[packet.FX])
        {
            var fxPrefab = FXManager.WorldFXMap[packet.FX];
            HandlingPacket = true;
            try
            {
                FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            }
            catch
            {
                // ignored
            }

            HandlingPacket = false;
        }
        else
        {
            var cue = FXManager.WorldAudioCueMap[packet.FX];
            RemoteFXManager.PlayTransientAudio(cue, packet.Position, WorldSoundVolumeDictionary[packet.FX]);
        }

        return true;
    }
}