using System.Net;
using SR2MP.Packets.FX;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BasePacketHandler<WorldFXPacket>
{
    public WorldFXHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(WorldFXPacket packet, IPEndPoint clientEp)
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

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}