using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.FX;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BaseClientPacketHandler<WorldFXPacket>
{
    public WorldFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(WorldFXPacket packet)
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
    }
}