using System.Reflection;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using MelonLoader;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.ModSupport;

/// <summary>
/// Integration with the Vacuum Modifications mod,
/// by Bread-Chan (Discord: .bread_chan. / 212243828831289344),
/// which adds custom slot limits and instant vac-transfers.
/// </summary>
internal static class VacuumModificationsIntegration
{
    private const string ModName = "Vacuum Modifications";
    private const string TransferMethodName = "tryTransferMaxAmount";

    private static bool initialized;
    private static bool installed;

    /// <summary>
    /// Whether the Vacuum Modifications mod is loaded or not.
    /// </summary>
    public static bool IsInstalled
    {
        get
        {
            Initialize();
            return installed;
        }
    }

    public static void Initialize()
    {
        if (initialized) return;
        initialized = true;

        try
        {
            var melon = MelonBase.RegisteredMelons.FirstOrDefault(mod => mod.Info.Name == ModName);
            if (melon == null) return;

            installed = true;

            var assembly = melon.GetType().Assembly;
            var transferMethod = FindTransferMethod(assembly);
            if (transferMethod == null)
            {
                SrLogger.LogWarning("Vacuum Modifications is installed but its transfer helper was not found, instant vac-transfer will not work");
                return;
            }
    
            new HarmonyLib.Harmony("SR2MP.VacuumModifications").Patch(transferMethod,
                prefix: new HarmonyMethod(typeof(VacuumModificationsIntegration), nameof(TransferPrefix)),
                postfix: new HarmonyMethod(typeof(VacuumModificationsIntegration), nameof(TransferPostfix)));
        }
        catch (Exception e)
        {
            SrLogger.LogWarning($"Vacuum Modifications integration failed: {e.Message}");
        }
    }

    private static MethodInfo? FindTransferMethod(Assembly assembly)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        // Type scan as fallback in case the helper moves in a future version.
        return assembly.GetType("VacuumModifications.Utils")?.GetMethod(TransferMethodName, flags)
               ?? AccessTools.GetTypesFromAssembly(assembly)
                   .Select(type => type.GetMethod(TransferMethodName, flags))
                   .FirstOrDefault(method => method != null);
    }

    private static void TransferPrefix(AmmoSlot? source, out int __state)
        => __state = source?.Count ?? 0;

    private static void TransferPostfix(AmmoSlot? source, AmmoSlot? target, bool __result, int __state)
    {
        if ((!Main.Client.IsConnected && !Main.Server.IsRunning) || HandlingPacket) return;

        if (!__result || source == null || target == null) return;

        var transferred = __state - source.Count;
        if (transferred <= 0) return;

        SendDecrement(source, transferred);
        SendAdd(target, transferred);
    }
    
    private static void SendDecrement(AmmoSlot slot, int count)
    {
        var id = slot.GetPlotID();
        var index = slot.GetSlotIndex();
        if (id == null || index == null) return;

        Main.SendToAllOrServer(new AmmoDecrementPacket
        {
            SlotIndex = index.Value,
            Count = count,
            ID = id
        });
    }

    private static void SendAdd(AmmoSlot slot, int count)
    {
        var id = slot.GetPlotID();
        var index = slot.GetSlotIndex();
        if (id == null || index == null || slot.Id == null) return;

        Main.SendToAllOrServer(new AmmoAddToSlotPacket
        {
            Identifiable = NetworkActorManager.GetPersistentID(slot.Id),
            SlotIndex = index.Value,
            Count = count,
            ID = id
        });
    }
}