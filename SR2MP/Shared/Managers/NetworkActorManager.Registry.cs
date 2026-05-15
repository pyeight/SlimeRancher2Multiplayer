using System.Net;
using SR2MP.Packets.Actor;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    internal void SendActorTypeRegistry(IPEndPoint clientEndPoint)
    {
        if (!Main.Server.IsRunning) return;

        var packet = new ActorTypeRegistryPacket
        {
            Registry = new Dictionary<int, string>(ActorTypes.Count)
        };

        foreach (var (persistentId, type) in ActorTypes)
        {
            if (type == null) continue;
            packet.Registry[persistentId] = type.ReferenceId;
        }

        Main.Server.SendToClient(packet, clientEndPoint);
    }
}