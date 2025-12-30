using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.Inventory)]
public sealed class InventoryHandler : BaseClientPacketHandler
{
    public InventoryHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<InventoryPacket>();

        if (packet.PlayerId == Main.Client.OwnPlayerId) return; // Should not happen but safety first

        var remotePlayer = playerManager.GetPlayer(packet.PlayerId);
        if (remotePlayer != null)
        {
            // Just update data for now. Visual sync will check this array.
            if (packet.SlotIdx >= 0 && packet.SlotIdx < remotePlayer.Inventory.Length)
            {
                remotePlayer.Inventory[packet.SlotIdx] = packet.ItemId;
            }
        }
    }
}
