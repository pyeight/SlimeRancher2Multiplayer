using SR2MP.Server.Models;

namespace SR2MP.Server.Managers;

public class PlayerInventoryManager
{
    private readonly Dictionary<string, PlayerInventoryData> playerInventories = new();

    public void UpdateInventory(string playerId, int slotIdx, int itemId, int count)
    {
        if (!playerInventories.ContainsKey(playerId))
        {
            playerInventories[playerId] = new PlayerInventoryData();
        }

        playerInventories[playerId].UpdateSlot(slotIdx, itemId, count);
    }

    public PlayerInventoryData GetInventory(string playerId)
    {
        if (playerInventories.TryGetValue(playerId, out var data))
        {
            return data;
        }
        return null;
    }

    public void Clear()
    {
        playerInventories.Clear();
    }
    
    public void RemovePlayer(string playerId)
    {
        playerInventories.Remove(playerId);
    }
}
