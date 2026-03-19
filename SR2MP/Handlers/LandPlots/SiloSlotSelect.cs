using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.LandPlots;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

[PacketHandler((byte)PacketType.SiloSlotSelect)]
public sealed class SiloSlotSelectHandler : BasePacketHandler<SiloSlotSelectPacket>
{
    protected override bool Handle(SiloSlotSelectPacket packet, IPEndPoint? _)
    {
        var model = GameState.landPlots[packet.ID];
        model.siloStorageIndices[packet.Side] = packet.Index;

        if (!model.gameObj) return true;

        model.gameObj.GetComponentsInChildren<SiloStorageActivator>()
            .FirstOrDefault(activator => activator.ActivatorIdx == packet.Side)?
            .OnActiveSlotChanged();

        return true;
    }
}