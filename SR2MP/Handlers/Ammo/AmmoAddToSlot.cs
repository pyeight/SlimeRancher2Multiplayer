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

        if (!ActorManager.ActorTypes.TryGetValue(packet.Identifiable, out var ident) || ident == null)
        {
            SrLogger.LogWarning($"AmmoAddToSlot: unknown identifiable type {packet.Identifiable}");
            return true;
        }

        if (packet.SlotIndex < 0 || ammo.Slots == null || packet.SlotIndex >= ammo.Slots.Count)
        {
            SrLogger.LogWarning($"AmmoAddToSlot: slot {packet.SlotIndex} out of range for '{packet.ID}' ({ammo.Slots?.Count ?? 0} slots)");
            return true;
        }

        HandlingPacket = true;
        ammo.MaybeAddToSpecificSlot(new AmmoSlot.AmmoMetadata(ident), packet.SlotIndex, packet.Count, false);
        HandlingPacket = false;

        return true;
    }
}