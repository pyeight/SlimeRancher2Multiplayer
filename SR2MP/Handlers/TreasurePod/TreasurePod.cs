using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.TreasurePod;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.TreasurePod;

[PacketHandler((byte)PacketType.TreasurePod)]
public sealed class TreasurePodHandler : BasePacketHandler<TreasurePodPacket>
{
    protected override bool Handle(TreasurePodPacket packet, IPEndPoint? _)
    {
        if (!GameState.pods.TryGetValue("pod" + packet.ID, out var model)) return true;

        HandlingPacket = true;
        model.gameObj?.GetComponent<Il2Cpp.TreasurePod>().Activate();
        HandlingPacket = false;

        model.state = new ObservableValue<Il2Cpp.TreasurePod.State>(
            Il2Cpp.TreasurePod.State.OPEN
        );

        return true;
    }
}