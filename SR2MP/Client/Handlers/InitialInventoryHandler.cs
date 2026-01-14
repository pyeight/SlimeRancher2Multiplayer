using SR2E.Utils;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialInventory)]
public sealed class InitialInventoryHandler : BaseClientPacketHandler
{
    public InitialInventoryHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager)
    {
    }

    public override void Handle(byte[] data)
    {
        SceneContext.Instance.PlayerState.Ammo.Clear();
            
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerInventoryPacket>();
        
        foreach (var ammSlot in packet.Slots)
        {
            var type = LookupEUtil.GetIdentifiableTypeByName(ammSlot.ItemName);
            
            for (int i = 0; i < ammSlot.Count; i++)
                SceneContext.Instance.PlayerState.Ammo.MaybeAddToSlot(type, null, type.GetAppearanceSet());
        }
    }
}