using LiteNetLib;

namespace SR2MP.Networking;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NetDeliveryAttribute : Attribute
{
    public DeliveryMethod Method { get; }
    public byte Channel { get; }

    public NetDeliveryAttribute(DeliveryMethod method, byte channel = NetChannels.Control)
    {
        Method = method;
        Channel = channel;
    }
}
