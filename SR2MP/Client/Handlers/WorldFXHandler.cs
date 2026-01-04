using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BaseClientPacketHandler
{
    public WorldFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void HandleClient(byte[] data)
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
    }
}