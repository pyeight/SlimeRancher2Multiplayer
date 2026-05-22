using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoDecrement)]
internal sealed class AmmoDecrementHandler : BasePacketHandler<AmmoDecrementPacket>
{
    protected override bool Handle(AmmoDecrementPacket packet, IPEndPoint? _)
    {
        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);

        if (ammo == null) return false;

        HandlingPacket = true;
        ammo.Decrement(packet.SlotIndex, packet.Count);
        HandlingPacket = false;

        return true;
    }
}