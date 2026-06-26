using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.PuzzleSlot)]
internal sealed class PuzzleSlotHandler : BasePacketHandler<PuzzleSlotPacket>
{
    protected override bool Handle(PuzzleSlotPacket packet, IPEndPoint? _)
    {
        if (GameState.slots.TryGetValue(packet.ID, out var model))
        {
            HandlingPacket = true;
            model.filled = packet.Filled;
            model.NotifyParticipants();
            if (model.gameObj)
            {
                var slot = model.gameObj.GetComponent<PuzzleSlot>();
                if (slot)
                    slot.OnFilledChangedFromModel();
            }
            HandlingPacket = false;
        }
        return true;
    }
}
