using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.WorldFX)]
public sealed class WorldFXHandler : BasePacketHandler
{
    public WorldFXHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
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

            fxManager.PlayTransientAudio(cue, packet.Position);
        }

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}