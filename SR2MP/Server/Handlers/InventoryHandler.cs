using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.Inventory)]
public sealed class InventoryHandler : BasePacketHandler
{
    public InventoryHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, string clientIdentifier)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<InventoryPacket>();

        // Update Server State
        Main.Server.playerInventoryManager.UpdateInventory(packet.PlayerId, packet.SlotIdx, packet.ItemId, packet.Count);

        // Rebroadcast to others (so they can see visuals eventually)
        Main.Server.SendToAllExcept(packet, clientIdentifier);
    }
}
