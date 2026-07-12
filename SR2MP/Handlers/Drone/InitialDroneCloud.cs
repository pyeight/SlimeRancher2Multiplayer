using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Drone;

[PacketHandler((byte)PacketType.InitialDroneCloud, HandlerType.Client)]
internal sealed class InitialDroneCloudHandler : BasePacketHandler<InitialDroneCloudPacket>
{
    protected override bool Handle(InitialDroneCloudPacket packet, IPEndPoint? _)
    {
        var cloud = GameState.droneModel.GetCloudModel();
        if (cloud == null)
            return false;

        HandlingPacket = true;

        foreach (var (typeId, amount) in packet.Amounts)
        {
            if (ActorManager.ActorTypes.TryGetValue(typeId, out var type) && type != null)
                cloud.SetAmount(type, amount);
        }

        HandlingPacket = false;
        return false;
    }
}
