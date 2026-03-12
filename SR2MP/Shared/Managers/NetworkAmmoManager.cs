using Il2CppMonomiPark.SlimeRancher.Player;

namespace SR2MP.Shared.Managers;

public static class NetworkAmmoManager
{
    public static void Initialize()
    {
        IDToAmmo.Clear();
        ammoToID.Clear();
        slotToAmmo.Clear();
        slotDefinitions.Clear();
        
        // Right now, I don't know where the definitions are actually stored,
        // but it isn't in the normal places like LookupDirector or SaveReferenceTranslation.
        //
        // However, I can see that only the definitions for plots or gadgets are loaded on the main menu,
        // so they are probably just stored where they are used only.
        // -PinkTarr
        foreach (var def in Resources.FindObjectsOfTypeAll<AmmoSlotDefinition>())
            slotDefinitions[def.name.Hash()] = def;
    }
    
    public static int GetNextSlot(this AmmoSlotManager ammo, IdentifiableType id)
    {
        for (int i = 0; i < ammo._ammoModel.Slots.Count; i++)
        {
            var isSlotEmptyOrSameType = ammo.Slots[i]!._count == 0 || ammo.Slots[i]!._id == id;

            var isSlotFull = ammo.Slots[i]!.Count >= ammo.Slots[i]!.MaxCount;

            if (isSlotEmptyOrSameType && isSlotFull) break;

            if (isSlotEmptyOrSameType)
                return i;
        }

        return -1;
    }

    private static readonly Dictionary<ushort, AmmoSlotDefinition> slotDefinitions = new();
    private static readonly Dictionary<IntPtr, string> ammoToID = new();
    private static readonly Dictionary<string, AmmoSlotManager> IDToAmmo = new();
    private static readonly Dictionary<IntPtr, (AmmoSlotManager ammo, int index)> slotToAmmo = new();

    public static string? GetPlotID(this AmmoSlotManager ammo)
    {
        if (ammoToID.TryGetValue(ammo.Pointer, out var id))
            return id;

        return null;
    }

    public static string? GetPlotID(this AmmoSlot slot)
    {
        if (slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple))
            return ammoTuple.ammo.GetPlotID();
        
        return null;
    }
    
    public static int? GetNextSlot(this AmmoSlot slot)
    {
        if (slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple))
            return ammoTuple.index;
        
        return null;
    }

    public static AmmoSlotManager? GetAmmo(this AmmoSlot slot)
    {
        if (slotToAmmo.TryGetValue(slot.Pointer, out var ammoTuple))
            return ammoTuple.ammo;
        
        return null;
    }

    public static AmmoSlotManager? GetAmmo(string id)
    {
        if (IDToAmmo.TryGetValue(id, out var ammo))
            return ammo;

        return null;
    }

    public static void ClearAmmoCache()
    {
        ammoToID.Clear();
        IDToAmmo.Clear();
        slotToAmmo.Clear();
    }

    private static void RegisterAmmoPointer(this AmmoSlotManager ammo, string id)
    {
        ammoToID[ammo.Pointer] = id;
        IDToAmmo[id] = ammo;
        
        for (var i = 0; i < ammo.Slots.Count; i++)
        {
            var slot = ammo.Slots[i];
            slotToAmmo[slot!.Pointer] = (ammo, i);
        }
    }

    public static void RegisterAmmoPointer(this SiloStorage silo)
    {
        LandPlotLocation plot = null!;
        Gadget gadget = null!;

        try { plot = silo.GetComponentInParent<LandPlotLocation>(); }
        catch { }
        
        try { gadget = silo.GetComponentInParent<Gadget>(); }
        catch { }

        if (plot != null)
            silo.Ammo.RegisterAmmoPointer($"{plot._id}_{silo.AmmoSetReference.name}");
        else if (gadget != null)
            silo.Ammo.RegisterAmmoPointer($"gadget{gadget.GetActorId()}_{silo.AmmoSetReference.name}");
        else
            SrLogger.LogWarning($"SiloStorage has no known parent type: {silo.name}", SrLogTarget.Both);
    }

    public static AmmoSlotDefinition GetSlotDefinition(ushort id)
    {
        return slotDefinitions[id];
    }

    public static ushort GetId(AmmoSlotDefinition def)
    {
        var hash = def.name.Hash();
        if (!slotDefinitions.ContainsKey(hash))
            slotDefinitions[hash] = def;
        return hash;
    }
}