using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.TreasurePod;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.TreasurePod;

[PacketHandler((byte)PacketType.InitialTreasurePods)]
public sealed class InitialTreasurePodsHandler : BasePacketHandler<InitialTreasurePodsPacket>
{
    protected override bool Handle(InitialTreasurePodsPacket packet, IPEndPoint? _)
    {
        foreach (var (podId, podState) in packet.TreasurePods)
        {
            if (!GameState.pods.TryGetValue("pod" + podId, out var model))
                continue;

            if (podState == Il2Cpp.TreasurePod.State.OPEN)
            {
                handlingPacket = true;
                model.gameObj?.GetComponent<Il2Cpp.TreasurePod>().Activate();
                handlingPacket = false;
            }
            model.state = new ObservableValue<Il2Cpp.TreasurePod.State>(podState);
        }

        return true;
    }
}