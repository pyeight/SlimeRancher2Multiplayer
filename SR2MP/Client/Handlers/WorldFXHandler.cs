using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.WorldFX)]
public class WorldFXHandler : BaseClientPacketHandler
{
    public WorldFXHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldFXPacket>();

        if (!IsWorldSoundDictionary[packet.FX])
        {
            var fxPrefab = fxManager.worldFXMap[packet.FX];

            handlingPacket = true;
            FXHelpers.SpawnAndPlayFX(fxPrefab, packet.Position, Quaternion.identity);
            handlingPacket = false;
        }
        else
        {
            var cue = fxManager.worldAudioCueMap[packet.FX];

            fxManager.PlayTransientAudio(cue, packet.Position, WorldSoundVolumeDictionary[packet.FX]);
        }
    }
}