using MelonLoader;
using SR2E.Utils;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Player;

[RegisterTypeInIl2Cpp(false)]
public sealed class PlayerInventory : MonoBehaviour
{
    private float syncTimer = Timers.PlayerInventoryTimer;

    private void Update()
    {
        if (Main.Server.IsRunning()) return;

        syncTimer -= UnityEngine.Time.unscaledDeltaTime;

        if (syncTimer >= 0) return;
        syncTimer = Timers.PlayerInventoryTimer;

        var slots = new List<PlayerAmmoSlot>();

        foreach (var slot in SceneContext.Instance.PlayerState.Ammo.Slots)
        {
            slots.Append(new PlayerAmmoSlot { Count = slot.Count, ItemName = slot.Id.name, });
        }

        var packet = new PlayerInventoryPacket
        {
            Type = PacketType.UpdateInventory, PlayerId = Main.Client.OwnPlayerId, Slots = slots,
        };
        Main.Client.SendPacket(packet);
    }
}