using System.Net;
using Il2CppMonomiPark.SlimeRancher.Player;
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

        if (!ActorManager.ActorTypes.TryGetValue(packet.Identifiable, out var ident) || ident == null)
        {
            SrLogger.LogWarning($"AmmoAdd: unknown identifiable type {packet.Identifiable}");
            return true;
        }
        
        var slotIndex = ammo.GetNextSlot(ident);
        if (slotIndex == -1)
            return true;

        HandlingPacket = true;
        ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ident), slotIndex, packet.Count, false);
        HandlingPacket = false;

        return true;
    }
}