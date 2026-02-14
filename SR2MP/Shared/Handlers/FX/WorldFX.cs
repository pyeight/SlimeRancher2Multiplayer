using System.Net;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers.FX;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BasePacketHandler<WorldFXPacket>
{
    public WorldFXHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(WorldFXPacket packet, IPEndPoint? _)
    {
        if (!IsWorldSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.WorldFXMap[packet.FX];
            handlingPacket = true;
            try { FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity); }
            catch { }
            handlingPacket = false;
        }
        else
        {
            var cue = fxManager.WorldAudioCueMap[packet.FX];
            RemoteFXManager.PlayTransientAudio(cue, packet.Position, WorldSoundVolumeDictionary[packet.FX]);
        }

        return true;
    }
}