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
                if (slotModel.gameObj)
                {
                    slotModel.filled = slot.Filled;
                
                    var comp = slotModel.gameObj.GetComponent<PuzzleSlot>();
                    if (comp)
                    {
                        if (slot.Filled)
                            comp.ActivateOnFill();
                    }
                
                    slotModel.NotifyParticipants();
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
