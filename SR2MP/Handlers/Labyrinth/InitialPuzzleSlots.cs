using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPuzzleSlots, HandlerType.Client)]
internal sealed class InitialPuzzleSlotsHandler : BasePacketHandler<InitialPuzzleSlotsPacket>
{
    protected override bool Handle(InitialPuzzleSlotsPacket packet, IPEndPoint? _)
    {

        foreach (var slot in packet.Slots)
        {
            if (GameState.slots.TryGetValue(slot.ID, out var slotModel))
            {
                slotModel.filled = slot.Filled;
                if (slotModel.gameObj)
                {
                    var comp = slotModel.gameObj.GetComponent<PuzzleSlot>();
                    if (comp)
                    {
                        comp.OnFilledChangedFromModel();
                    }
                }
            }
            else
            {
                slotModel = new PuzzleSlotModel
                {
                    gameObj = null,
                    filled = slot.Filled
                };
                GameState.slots.Add(slot.ID, slotModel);
            }
        }

        return false;
    }
}
