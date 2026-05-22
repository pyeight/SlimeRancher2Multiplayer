using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAddToSlot)]
internal sealed class AmmoAddToSlotHandler : BasePacketHandler<AmmoAddToSlotPacket>
{
    protected override bool Handle(AmmoAddToSlotPacket packet, IPEndPoint? _)
    {
        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);

        if (ammo == null) return false;

        HandlingPacket = true;
        ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ActorManager.ActorTypes[packet.Identifiable]), packet.SlotIndex, packet.Count, false);
        HandlingPacket = false;

        return true;
    }
}