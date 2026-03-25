using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAdd)]
internal sealed class AmmoAddHandler : BasePacketHandler<AmmoAddPacket>
{
    protected override bool Handle(AmmoAddPacket packet, IPEndPoint? _)
    {
        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);

        if (ammo == null) return false;
        var ident = ActorManager.ActorTypes[packet.Identifiable];
        HandlingPacket = true;
        ammo.MaybeAddToSpecificSlot(ident, null, ammo.GetNextSlot(ident));
        HandlingPacket = false;

        return true;
    }
}