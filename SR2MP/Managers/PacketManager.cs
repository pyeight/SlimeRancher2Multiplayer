using System.Net;
using System.Reflection;
using SR2MP.Packets.Utils;

namespace SR2MP.Managers;

public sealed class PacketManager
{
    private readonly Dictionary<byte, IPacketHandler> handlers = new();
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;

    public PacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                    && typeof(IPacketHandler).IsAssignableFrom(type)
                    && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                if (Activator.CreateInstance(type, networkManager, clientManager) is IPacketHandler handler)
                {
                    handlers[attribute.PacketType] = handler;
                    Logger.LogMessage($"Registered handler: {type.Name} for packet type {attribute.PacketType}", Logger.LogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to register handler {type.Name}: {ex}", Logger.LogTarget.Both);
            }
        }

        Logger.LogMessage($"Total handlers registered: {handlers.Count}", Logger.LogTarget.Both);
    }

    public void HandlePacket(byte[] data, IPEndPoint clientEP)
    {
        if (data.Length < 1)
        {
            Logger.LogWarning("Received empty packet", Logger.LogTarget.Both);
            return;
        }

        byte packetType = data[0];

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                handler.Handle(data, clientEP);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error handling packet type {packetType}: {ex}", Logger.LogTarget.Both);
            }
        }
        else
        {
            Logger.LogWarning($"No handler found for packet type: {packetType}", Logger.LogTarget.Both);
        }
    }
}