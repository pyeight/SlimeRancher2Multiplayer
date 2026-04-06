using System.Collections.Concurrent;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SR2MP.Packets.Utils;

namespace SR2MP.Api;

/// <summary>
/// Holds all API-related registrations for a specific assembly.
/// </summary>
internal sealed class ApiHolder
{
    public readonly Assembly Assembly;
    public readonly ushort ModId;

    public readonly ConcurrentDictionary<byte, IClientPacketHandler> ClientHandlers = new();
    public readonly ConcurrentDictionary<byte, IServerPacketHandler> ServerHandlers = new();

    public ApiHolder(Assembly assembly, ushort modId)
    {
        Assembly = assembly;
        ModId = modId;
    }
}

/// <summary>
/// A registry class to handle API usage by other projects.
/// </summary>
[PublicAPI]
public static class ApiHandlers
{
    internal static readonly ConcurrentDictionary<ushort, ApiHolder> Holders = new();
    internal static readonly ConcurrentDictionary<Type, ushort> PacketTypeMap = new();
    internal static readonly ConcurrentDictionary<string, ushort> AssemblyIdMap = new(StringComparer.Ordinal);

    private static readonly HashSet<Type> RegisteredTypes = new();

    /// <summary>
    /// Registers all custom packet handlers and types to the multiplayer API for the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to register handlers and packet types from.</param>
    /// <returns>The registered assembly's network id.</returns>
    public static ushort RegisterHandlers(Assembly assembly)
    {
        var gotName = assembly.GetName();
        var name = gotName.Name;
        var fullName = assembly.FullName ?? gotName.FullName;
        var modId = ComputeDeterministicModId(fullName);

        // Fast path: assembly already registered with same id
        if (Holders.TryGetValue(modId, out var existingHolder))
        {
            if (ReferenceEquals(existingHolder.Assembly, assembly) ||
                string.Equals(existingHolder.Assembly.FullName, fullName, StringComparison.Ordinal))
            {
                RegisterCustomPackets(AccessTools.GetTypesFromAssembly(assembly).Where(type => !type.IsAbstract), existingHolder);

                SrLogger.LogMessage(
                    $"[{name}] API handlers already registered for ModId {modId}; refreshed custom packet mappings.");
                return modId;
            }

            // Rare deterministic hash collision against a different assembly
            // Keep deterministic behavior and fail loudly for safety
            throw new InvalidOperationException(
                $"ModId collision detected for id {modId}. " +
                $"Assembly '{fullName}' collides with already registered assembly '{existingHolder.Assembly.FullName}'. " +
                "Please change assembly identity (name/version/public key token) to avoid collision.");
        }

        var holder = new ApiHolder(assembly, modId);

        // Ensure only one thread wins registration for this ModId.
        if (!Holders.TryAdd(modId, holder))
            return modId;

        AssemblyIdMap[fullName] = modId;

        var allTypes = AccessTools.GetTypesFromAssembly(assembly)
            .Where(type => !type.IsAbstract)
            .ToArray();

        RegisterPacketHandlers(allTypes, holder);
        RegisterCustomPackets(allTypes, holder);

        SrLogger.LogMessage($"[{name}] ModId: {modId}");
        SrLogger.LogMessage($"[{name}] Client handlers registered: {holder.ClientHandlers.Count}");
        SrLogger.LogMessage($"[{name}] Server handlers registered: {holder.ServerHandlers.Count}");
        return modId;
    }

    /// <summary>
    /// Registers custom serialization logic for a type, allowing third-party types to be used seamlessly.
    /// </summary>
    public static void RegisterCustomTypeSerialisation<T>(Func<PacketReader, T> reader, Action<PacketWriter, T> writer)
    {
        if (!RegisteredTypes.Add(typeof(T)))
        {
            SrLogger.LogWarning(typeof(T).Name + " is already registered!");
            return;
        }

        PacketReaderDels.Object<T>.Reader = reader;
        PacketWriterDels.Object<T>.Writer = writer;
    }

    /// <summary>
    /// Computes a deterministic 16-bit ModId from assembly fully qualified name.<br/>
    /// Uses FNV-1a 32-bit and folds to 16-bit to avoid arithmetic overflow exceptions and keep deterministic results.
    /// </summary>
    private static ushort ComputeDeterministicModId(string assemblyFullName)
    {
        unchecked
        {
            const uint fnvOffset = 2166136261u;
            const uint fnvPrime = 16777619u;

            var hash = fnvOffset;

            foreach (var ch in assemblyFullName)
            {
                hash ^= ch;
                hash *= fnvPrime;
            }

            // Fold 32 -> 16
            return (ushort)(hash ^ (hash >> 16));
        }
    }

    private static void RegisterPacketHandlers(IEnumerable<Type> allTypes, ApiHolder holder)
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

    private static void RegisterCustomPackets(IEnumerable<Type> allTypes, ApiHolder holder)
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