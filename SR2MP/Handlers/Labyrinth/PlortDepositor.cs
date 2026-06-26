using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.PlortDepositor)]
internal sealed class PlortDepositorHandler : BasePacketHandler<PlortDepositorPacket>
{
    protected override bool Handle(PlortDepositorPacket packet, IPEndPoint? _)
    {
        if (GameState.depositors.TryGetValue(packet.ID, out var model))
        {
            HandlingPacket = true;
            model.AmountDeposited = packet.AmountDeposited;
            model.NotifyParticipants();
            if (model._gameObject)
            {
                var depositor = model._gameObject.GetComponent<PlortDepositor>();
                if (depositor)
                    depositor.OnFilledChangedFromModel();
            }
            HandlingPacket = false;
        }
        return true;
    }
}
