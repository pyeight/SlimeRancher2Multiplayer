using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorTypeRegistry, HandlerType.Client)]
internal sealed class ActorTypeRegistryHandler : BasePacketHandler<ActorTypeRegistryPacket>
{
    protected override bool Handle(ActorTypeRegistryPacket packet, IPEndPoint? _)
    {
        if (Main.Server.IsRunning) return false;
        
        ActorManager.ActorTypes[-1] = null!;

        foreach (var (persistentId, referenceId) in packet.Registry)
        {
            var type = ActorManager.ActorTypes.Values
                .FirstOrDefault(type => type?.ReferenceId == referenceId);

            if (type == null)
            {
                SrLogger.LogWarning(
                    $"[ActorTypeRegistry] Unknown IdentifiableType: referenceId={referenceId} persistentId={persistentId}");
                continue;
            }

            ActorManager.ActorTypes[persistentId] = type;
        }
        
        return true;
    }
}