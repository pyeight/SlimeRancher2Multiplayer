using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;
using Unity.Mathematics;

namespace SR2MP.Api;

/// <summary>
/// Holds all API-related registrations for a specific assembly.
/// </summary>
internal sealed class ApiHolder
{
    public readonly MelonMod Mod;
    public readonly uint ModId;

    public readonly ConcurrentDictionary<byte, IClientPacketHandler> ClientHandlers = new();
    public readonly ConcurrentDictionary<byte, IServerPacketHandler> ServerHandlers = new();

    public ApiHolder(MelonMod mod, uint modId)
    {
        Mod = mod;
        ModId = modId;
    }
}

/// <summary>
/// A registry class to handle API usage by other projects.
/// </summary>
[PublicApi]
public static class ApiHandlers
{
    internal static readonly ConcurrentDictionary<uint, ApiHolder> Holders = new();
    internal static readonly ConcurrentDictionary<Type, uint> PacketTypeMap = new();
    internal static readonly ConcurrentDictionary<byte, uint> CurrentNetIdMapping = new();
    internal static readonly ConcurrentDictionary<uint, byte> CurrentNetIdMappingReverse = new();
    internal static readonly ConcurrentDictionary<Type, byte> CurrentNetIdMapping2 = new();

    private static byte NextNetId;

    internal static readonly HashSet<uint> SharedSideMods = new();

    private static readonly ConcurrentDictionary<Type, bool> RegisteredTypes = new()
    {
        // C# Primitives & Basic Types
        [typeof(bool)]    = true,
        [typeof(byte)]    = true, [typeof(sbyte)]  = true,
        [typeof(short)]   = true, [typeof(ushort)] = true,
        [typeof(int)]     = true, [typeof(uint)]   = true,
        [typeof(long)]    = true, [typeof(ulong)]  = true,
        [typeof(float)]   = true, [typeof(double)] = true,
        [typeof(char)]    = true, [typeof(string)] = true,
        [typeof(decimal)] = true,

        // System Structs
        [typeof(DateTime)] = true, [typeof(TimeSpan)] = true,
        [typeof(Guid)]     = true, [typeof(Half)]     = true,

        // Unity & Mathematics Types
        [typeof(Vector2)]    = true, [typeof(Vector3)] = true,
        [typeof(Quaternion)] = true, [typeof(float4)]  = true,
        [typeof(Color)]      = true, [typeof(Color32)] = true,
    };

    /// <summary>
    /// Registers all custom packet handlers and types to the multiplayer API for the given assembly.
    /// </summary>
    /// <param name="mod">The mod to register handlers and packet types from.</param>
    /// <param name="modSide">The network side on which the mod operates.</param>
    /// <returns>The registered mod's network id.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a collision occurs.</exception>
    public static uint RegisterMod(MelonMod mod, ModSide modSide = ModSide.Shared)
    {
        var name = mod.Info.Name;
        var assembly = mod.MelonAssembly.Assembly;

        var modId = HashCalculator.ComputeHashOfString(mod.MelonAssembly.Hash);

        // Assembly already registered with same id
        if (Holders.TryGetValue(modId, out var existingHolder))
        {
            // Check if it's actually the same assembly or a true collision
            if (existingHolder.Mod.MelonAssembly.Assembly != assembly)
                throw new InvalidOperationException($"Hash Collision: {name} vs {existingHolder.Mod.Info.Name}");

            return modId; // Already registered
        }

        var holder = new ApiHolder(mod, modId);

        // Ensure only one thread wins registration for this ModId.
        if (!Holders.TryAdd(modId, holder))
            return modId;

        if (modSide is ModSide.Shared or ModSide.None) // None = Assume shared
            SharedSideMods.Add(modId);

        SrLogger.LogMessage($"[{name}] ModId: {modId}");

        if (modSide != ModSide.None) // None = not voluntarily registered; do not register types if that is the case
        {
            var allTypes = AccessTools.GetTypesFromAssembly(assembly)
                .Where(type => !type.IsAbstract)
                .ToArray();

            RegisterPacketHandlers(allTypes, holder);
            RegisterCustomPackets(allTypes, holder);

            SrLogger.LogMessage($"[{name}] Client handlers registered: {holder.ClientHandlers.Count}");
            SrLogger.LogMessage($"[{name}] Server handlers registered: {holder.ServerHandlers.Count}");
        }

        return modId;
    }

    /// <summary>
    /// Registers custom serialization logic for a type, allowing third-party types to be used seamlessly. Use PacketWriterDels.Object and PacketReaderDels.Object to access the logic.
    /// </summary>
    /// <typeparam name="T">The type whose logic is being registered.</typeparam>
    /// <param name="reader">The reader delegate.</param>
    /// <param name="writer">The writer delegate.</param>
    /// <remarks>If you have a special way to serialise a value that's already registered or supported natively, it's recommended that you create a simple wrapper struct or INetObject and serialise that instead!</remarks>
    public static void RegisterCustomTypeSerialisation<T>(ReadDel<T> reader, WriteDel<T> writer)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);

        var type = typeof(T);

        if (type.IsEnum || typeof(INetObject).IsAssignableFrom(type) || type.IsTuple() || Nullable.GetUnderlyingType(type) != null)
        {
            SrLogger.LogWarning($"Cannot register {type.Name}. Enums, INetObjects, Tuples and Nullables are already handled natively!");
            return;
        }

        if (!RegisteredTypes.TryAdd(type, true))
        {
            SrLogger.LogWarning(type.Name + " is already registered/supported! If you need custom serialisation for an existing type (be it already registered or natively supported), wrap it in a custom struct or INetObject instead.");
            return;
        }

        PacketReaderDels.Object<T>._reader = reader;
        PacketWriterDels.Object<T>._writer = writer;
    }

    /// <summary>
    /// Pre-caches the delegates for reading and writing values to avoid the overhead of delegate creation when the value is transmitted/read for the first time.
    /// </summary>
    /// <typeparam name="T">The type to warm up.</typeparam>
    public static void WarmUp<T>()
    {
        _ = PacketReaderDels.Object<T>._reader;
        _ = PacketWriterDels.Object<T>._writer;
    }

    private static bool IsTuple(this Type type)
    {
        if (!typeof(ITuple).IsAssignableFrom(type) || !type.IsGenericType)
            return false;

        var genericTypeDef = type.GetGenericTypeDefinition();

        if (type.IsValueType)
        {
            return genericTypeDef == typeof(ValueTuple<>) ||
                    genericTypeDef == typeof(ValueTuple<,>) ||
                    genericTypeDef == typeof(ValueTuple<,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,,,,>) ||
                    genericTypeDef == typeof(ValueTuple<,,,,,,,>);
        }

        return genericTypeDef == typeof(Tuple<>) ||
                genericTypeDef == typeof(Tuple<,>) ||
                genericTypeDef == typeof(Tuple<,,>) ||
                genericTypeDef == typeof(Tuple<,,,>) ||
                genericTypeDef == typeof(Tuple<,,,,>) ||
                genericTypeDef == typeof(Tuple<,,,,,>) ||
                genericTypeDef == typeof(Tuple<,,,,,,>) ||
                genericTypeDef == typeof(Tuple<,,,,,,,>);
    }

    internal static byte GetOrIncrementNetId(uint modId)
    {
        if (CurrentNetIdMappingReverse.TryGetValue(modId, out var netId))
            return netId;

        netId = NextNetId++;
        CurrentNetIdMapping[netId] = modId;
        CurrentNetIdMappingReverse[modId] = netId;
        return netId;
    }

    internal static void SetNetId(uint modId, byte netId)
    {
        CurrentNetIdMapping[netId] = modId;
        CurrentNetIdMappingReverse[modId] = netId;
    }

    internal static void RefreshPacketMapping()
    {
        foreach (var (type, modId) in PacketTypeMap)
        {
            CurrentNetIdMapping2[type] = CurrentNetIdMappingReverse[modId];
        }
    }

    internal static void ClearNetIds()
    {
        NextNetId = 0;
        CurrentNetIdMapping.Clear();
        CurrentNetIdMapping2.Clear();
        CurrentNetIdMappingReverse.Clear();
    }

    private static void RegisterPacketHandlers(Type[] allTypes, ApiHolder holder)
    {
        foreach (var type in allTypes.Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null))
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>()!;

            try
            {
                CreateHandler(type, attribute, HandlerType.Server, false, holder.ClientHandlers);
                CreateHandler(type, attribute, HandlerType.Client, true, holder.ServerHandlers);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register handler {type.FullName}: {ex}");
            }
        }
    }

    private static void RegisterCustomPackets(Type[] allTypes, ApiHolder holder)
    {
        var customPacketTypes = allTypes.Where(type =>
            typeof(ICustomPacket).IsAssignableFrom(type) &&
            type is { IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var packetType in customPacketTypes)
        {
            try
            {
                PacketTypeMap[packetType] = holder.ModId;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register custom packet type {packetType.FullName}: {ex}");
            }
        }
    }

    private static void CreateHandler<T>(Type type, PacketHandlerAttribute attribute, HandlerType exclude, bool isServerSide, ConcurrentDictionary<byte, T> handlers)
        where T : IPacketHandler
    {
        if (attribute.HandlerType == exclude) return;
        if (Activator.CreateInstance(type) is not T handler) return;

        handlers[attribute.PacketType] = handler;
        handler.IsServerSide = isServerSide;

        SrLogger.LogMessage(
            $"Registered {(isServerSide ? "server" : "client")} handler: {type.Name} for packet type {attribute.PacketType}");
    }
}