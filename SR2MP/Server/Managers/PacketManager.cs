using System.Reflection;
using LiteNetLib;
using LiteNetLib.Utils;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

public sealed class PacketManager
{
    private readonly ClientManager clientManager;
    private readonly NetPacketProcessor processor = new();

    public PacketManager(ClientManager clientManager)
    {
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => !type.IsAbstract && IsSubclassOfRawGeneric(typeof(BasePacketHandler<>), type));

        foreach (var type in handlerTypes)
        {
            try
            {
                var handler = Activator.CreateInstance(type, clientManager);
                if (handler == null)
                    continue;

                RegisterHandler(handler);
                SrLogger.LogMessage($"Registered server handler: {type.Name}", SrLogTarget.Both);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to register handler {type.Name}: {ex}", SrLogTarget.Both);
            }
        }

        SrLogger.LogMessage("Server packet handlers registered", SrLogTarget.Both);
    }

    public void Handle(NetDataReader reader, NetPeer peer)
    {
        try
        {
            processor.ReadAllPackets(reader, peer);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling packet from {peer.Address}:{peer.Port}: {ex}", SrLogTarget.Both);
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
        var registerMethod = typeof(PacketManager).GetMethod(nameof(RegisterHandlerGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
        if (registerMethod == null)
            return;

        var generic = registerMethod.MakeGenericMethod(packetType);
        generic.Invoke(this, new[] { handler });
    }

    private void RegisterHandlerGeneric<T>(BasePacketHandler<T> handler) where T : PacketBase, new()
    {
        processor.SubscribeNetSerializable<T, NetPeer>((packet, peer) => handler.Handle(packet, peer), () => new T());
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
