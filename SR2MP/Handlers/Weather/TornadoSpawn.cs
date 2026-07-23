using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.TornadoSpawn, HandlerType.Client)]
internal sealed class TornadoSpawnHandler : BasePacketHandler<TornadoSpawnPacket>
{
    protected override bool Handle(TornadoSpawnPacket packet, IPEndPoint? _)
    {
        if (packet.Despawn)
        {
            NetworkTornadoManager.PendingTornados.Remove(packet.TornadoId);

            if (NetworkTornadoManager.ClientTornados.TryGetValue(packet.TornadoId, out var existing))
            {
                NetworkTornadoManager.ClientTornados.Remove(packet.TornadoId);
                
                try
                {
                    if (existing) Object.Destroy(existing);
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"TornadoSpawnHandler: failed to despawn tornado {packet.TornadoId}: {ex.Message}");
                }
            }
            return false;
        }
        
        if (NetworkTornadoManager.PlayerInDistance(packet.Position))
        {
            NetworkTornadoManager.SpawnTornado(packet.TornadoId, packet.Position, packet.Rotation);
        }
        else
        {
            NetworkTornadoManager.PendingTornados[packet.TornadoId] = new NetworkTornadoManager.PendingTornado
            {
                Position = packet.Position,
                Rotation = packet.Rotation,
                ReceivedAt = UnityEngine.Time.unscaledTime
            };
        }

        return false;
    }
}
