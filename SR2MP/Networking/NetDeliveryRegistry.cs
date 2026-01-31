using LiteNetLib;

namespace SR2MP.Networking;

public readonly struct NetDeliveryInfo
{
    public DeliveryMethod Method { get; }
    public byte Channel { get; }

    public NetDeliveryInfo(DeliveryMethod method, byte channel)
    {
        Method = method;
        Channel = channel;
    }
}

public static class NetDeliveryRegistry
{
    private static readonly Dictionary<Type, NetDeliveryInfo> Cache = new();

    public static NetDeliveryInfo Get<T>() => Get(typeof(T));

    public static NetDeliveryInfo Get(Type packetType)
    {
        if (Cache.TryGetValue(packetType, out var info))
            return info;

        var attribute = packetType.GetCustomAttributes(typeof(NetDeliveryAttribute), false)
            .Cast<NetDeliveryAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            SrLogger.LogWarning($"Packet {packetType.Name} missing NetDeliveryAttribute. Defaulting to ReliableOrdered/Control.", SrLogTarget.Both);
            info = new NetDeliveryInfo(DeliveryMethod.ReliableOrdered, NetChannels.Control);
        }
        else
        {
            info = new NetDeliveryInfo(attribute.Method, attribute.Channel);
        }

        Cache[packetType] = info;
        return info;
    }
}
