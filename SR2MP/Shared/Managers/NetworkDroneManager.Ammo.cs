using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using SR2MP.Packets.Ammo;

namespace SR2MP.Shared.Managers;

internal static partial class NetworkDroneManager
{
    internal static NetworkAmmo? CreateDroneAmmo(RanchDroneModel? droneModel)
    {
        var ammoModel = droneModel?.Ammo;
        if (ammoModel?.Slots == null)
            return null;

        var ammoSlots = new Dictionary<int, NetworkAmmoSlot>();
        var slots = ammoModel.Slots;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot?.Definition == null)
                continue;

            var hasItem = slot._id != null;

            ammoSlots[i] = new NetworkAmmoSlot
            {
                Count = hasItem ? slot.Count : 0,
                MaxCount = slot.MaxCount,
                Identifiable = hasItem ? NetworkActorManager.GetPersistentID(slot._id!) : -1,
                SlotDefinition = NetworkAmmoManager.GetId(slot.Definition)
            };
        }

        return new NetworkAmmo { AmmoSlots = ammoSlots };
    }

    internal static int GetAmmoHash(NetworkAmmo? ammo)
    {
        if (ammo == null)
            return 0;

        var hash = 17;
        foreach (var (index, slot) in ammo.AmmoSlots)
            hash = (hash * 31) + (index * 7) + (slot.Identifiable * 13) + slot.Count;

        return hash;
    }

    internal static void ApplyDroneAmmo(RanchDrone drone, NetworkAmmo ammo)
    {
        var ammoModel = drone._model?.Ammo;
        if (ammoModel?.Slots == null)
            return;

        var slots = ammoModel.Slots;

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            if (ammo.AmmoSlots.TryGetValue(i, out var netSlot) &&
                netSlot.Identifiable != -1 &&
                netSlot.Count > 0 &&
                ActorManager.ActorTypes.TryGetValue(netSlot.Identifiable, out var identType) &&
                identType != null)
            {
                slot._id = identType;
                slot._count = netSlot.Count;
            }
            else
            {
                slot._id = null;
                slot._count = 0;
            }
        }

        UpdateAmmoDisplay(drone, ammoModel);
    }

    private static void UpdateAmmoDisplay(RanchDrone drone, AmmoModel ammoModel)
    {
        var display = drone.GetComponentInChildren<RanchDroneAmmoDisplay>(true);
        if (!display)
            return;

        IdentifiableType? carriedType = null;
        var slots = ammoModel.Slots;

        foreach (var slot in slots)
        {
            if (slot?._id == null || slot._count <= 0)
                continue;

            carriedType = slot._id;
            break;
        }

        if (carriedType != null)
        {
            if (display!._currentType != carriedType)
                display.SetAmmo(carriedType);
        }
        else if (display!._currentType != null)
        {
            display.Hide();
            display._currentType = null;
        }
    }
}
