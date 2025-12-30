namespace SR2MP.Server.Models;

public class PlayerInventoryData
{
    public struct SlotData
    {
        public int ItemId;
        public int Count;
    }

    // Standard SR2 inventory has 4 slots
    public SlotData[] Slots = new SlotData[4];

    public void UpdateSlot(int slotIdx, int itemId, int count)
    {
        if (slotIdx >= 0 && slotIdx < Slots.Length)
        {
            Slots[slotIdx] = new SlotData { ItemId = itemId, Count = count };
        }
    }
}
