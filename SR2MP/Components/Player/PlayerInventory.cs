using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Player;

public class PlayerInventory : MonoBehaviour
{
    private float syncTimer = Timers.PlayerInventoryTimer;
    
    private void Update()
    {
        syncTimer -= UnityEngine.Time.unscaledDeltaTime;
        
        if (syncTimer >= 0) return;
        syncTimer = Timers.PlayerInventoryTimer;

        foreach (var slot in SceneContext.Instance.PlayerState.Ammo.Slots)
        {
        }
        var packet = new PlayerInventoryPacket
        {
            Type = PacketType.UpdateInventory, PlayerId = Main.Client.OwnPlayerId,
        };
        Main.Client.SendPacket(packet);
    }
}