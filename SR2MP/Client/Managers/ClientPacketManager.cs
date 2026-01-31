using System.Reflection;
using LiteNetLib.Utils;
using SR2MP.Client.Handlers;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Client.Managers;

public sealed class ClientPacketManager
{
    private readonly Client client;
    private readonly RemotePlayerManager playerManager;
    private readonly NetPacketProcessor processor = new();

    public ClientPacketManager(Client client, RemotePlayerManager playerManager)
    {
        this.client = client;
        this.playerManager = playerManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => !type.IsAbstract && IsSubclassOfRawGeneric(typeof(BaseClientPacketHandler<>), type));

        foreach (var type in handlerTypes)
        {
            try
            {
                var handler = Activator.CreateInstance(type, client, playerManager);
                if (handler == null)
                    continue;

                RegisterHandler(handler);
                SrLogger.LogMessage($"Registered client handler: {type.Name}", SrLogTarget.Both);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to register client handler {type.Name}: {ex}", SrLogTarget.Both);
            }
        }

        SrLogger.LogMessage("Client packet handlers registered", SrLogTarget.Both);
    }

    public void Handle(NetDataReader reader)
    {
        try
        {
            processor.ReadAllPackets(reader);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling client packet: {ex}", SrLogTarget.Both);
        }
    }

    public NetPacketProcessor Processor => processor;

    private void RegisterHandler(object handler)
    {
        var handlerType = handler.GetType();
        var baseType = handlerType.BaseType;
        if (baseType == null || !baseType.IsGenericType)
            return;

        var packetType = baseType.GetGenericArguments()[0];
        var registerMethod = typeof(ClientPacketManager).GetMethod(nameof(RegisterHandlerGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
        if (registerMethod == null)
            return;

        var generic = registerMethod.MakeGenericMethod(packetType);
        generic.Invoke(this, new[] { handler });
    }

    private void RegisterHandlerGeneric<T>(BaseClientPacketHandler<T> handler) where T : PacketBase, new()
    {
        processor.SubscribeNetSerializable<T>(handler.Handle, () => new T());
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
                return true;
            toCheck = toCheck.BaseType;
        }
        return false;
    }
}