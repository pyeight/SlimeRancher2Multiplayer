using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Packets.Ammo;

public sealed class NetworkAmmo : INetObject
{
    public Dictionary<int, NetworkAmmoSlot> AmmoSlots = new();

    public void Serialise(PacketWriter writer)
    {
        writer.WriteDictionary(AmmoSlots, PacketWriterDels.PackedInt32, PacketWriterDels.NetObject<NetworkAmmoSlot>.Func);
    }

    public void Deserialise(PacketReader reader)
    {
        AmmoSlots = reader.ReadDictionary(PacketReaderDels.PackedInt32, PacketReaderDels.NetObject<NetworkAmmoSlot>.Func);
    }

    public AmmoSlotManager ToGameAmmo()
    {
        var definitions = AmmoSlots.Values.ToList()
            .ConvertAll(input => NetworkAmmoManager.GetSlotDefinition(input.SlotDefinition)).ToArray();
        var ammo = new AmmoSlotManager(definitions);
        ammo.InitModel(new AmmoModel(null));
        ammo.SetModel(new AmmoModel(null));

        ammo._ammoModel.Slots = new Il2CppReferenceArray<AmmoSlot>(
            Array.ConvertAll(AmmoSlots.Values.ToArray(), input => input.ToGameAmmoSlot()));

        return ammo;
    }
}

public struct NetworkAmmoSlot : INetObject
{
    public int Identifiable;
    public int Count;
    // Only use for converting into actual ammo!
    // public float MaxCount;

    public ushort SlotDefinition;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Identifiable);
        writer.WritePackedInt(Count);
        // writer.WriteFloat(MaxCount);
        writer.WriteUShort(SlotDefinition);
    }

    public void Deserialise(PacketReader reader)
    {
        Identifiable = reader.ReadPackedInt();
        Count = reader.ReadPackedInt();
        // MaxCount = reader.ReadFloat();
        SlotDefinition = reader.ReadUShort();
    }

    public readonly AmmoSlot ToGameAmmoSlot() => new()
    {
        _count = Count,
        _id = ActorManager.ActorTypes[Identifiable],
        // _isUnlockedValue = new NullableFloatProperty(1),
        // _maxCountValue = new NullableFloatProperty(MaxCount),
    };
}