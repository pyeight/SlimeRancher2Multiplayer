using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.PuzzleSlot)]
internal sealed class PuzzleSlotHandler : BasePacketHandler<PuzzleSlotPacket>
{
    protected override bool Handle(PuzzleSlotPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        if (GameState.slots.TryGetValue(packet.ID, out var model))
        {
            model.filled = packet.Filled;

            if (model.gameObj)
            {
                var slot = model.gameObj.GetComponent<PuzzleSlot>();
                if (slot && packet.Filled)
                    slot!.ActivateOnFill();

                model.NotifyParticipants();
            }
        }
        else
        {
            GameState.slots.Add(packet.ID, new PuzzleSlotModel
            {
                gameObj = null,
                filled = packet.Filled
            });
        }

        HandlingPacket = false;
        return true;
    }
}
