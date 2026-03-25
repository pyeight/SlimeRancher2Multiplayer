using System.Reflection;
using HarmonyLib;
using SR2MP.Packets.Utils;

namespace SR2MP.Api;

public static class ApiHandlers
{
    internal static readonly Dictionary<byte, IClientPacketHandler> ClientHandlers = new();
    internal static readonly Dictionary<byte, IServerPacketHandler> ServerHandlers = new();

    /// <summary>
    /// Registers all custom packet handlers to the multiplayer API.
    /// </summary>
    /// <param name="assembly">The assembly to register handlers from.</param>
    // ReSharper disable once UnusedMember.Global
    public static void RegisterHandlers(Assembly assembly)
    {
        var handlerTypes = AccessTools.GetTypesFromAssembly(assembly)
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                        && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>()!;

            try
            {
                CreateHandler(type, attribute, HandlerType.Server, false, ClientHandlers);
                CreateHandler(type, attribute, HandlerType.Client, true, ServerHandlers);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register handler {type.Name}: {ex}");
            }
        }

        SrLogger.LogMessage($"Total client packet handlers registered: {ClientHandlers.Count}");
        SrLogger.LogMessage($"Total server packet handlers registered: {ServerHandlers.Count}");
    }

    private static void CreateHandler<T>(Type type, PacketHandlerAttribute attribute, HandlerType exclude, bool isServerSide, Dictionary<byte, T> handlers) where T : IPacketHandler
    {
        if (attribute.HandlerType == exclude) return;
        if (Activator.CreateInstance(type) is not T handler)
            return;

        handlers[attribute.PacketType] = handler;
        handler.IsServerSide = isServerSide;
        SrLogger.LogMessage($"Registered {(isServerSide ? "server" : "client")} handler: {type.Name} for packet type {attribute.PacketType}");
    }
}