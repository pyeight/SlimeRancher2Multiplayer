using SR2MP.Packets.Utils;
using System.Net;
using System.Runtime.CompilerServices;

namespace SR2MP.Shared.Handlers.Internal;

public abstract class BasePacketHandler<T> : IClientPacketHandler, IServerPacketHandler where T : IPacket, new()
{
    public bool IsServerSide { protected get; set; }

    public void Handle(byte[] data)
    {
        if (!IsServerSide)
            ProcessPacket(data, null);
    }

    public void Handle(byte[] data, IPEndPoint clientEp)
    {
        if (IsServerSide)
            ProcessPacket(data, clientEp);
    }

    private void ProcessPacket(byte[] data, IPEndPoint? clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<T>();
        var shouldSend = Handle(packet, clientEp);

        if (IsServerSide && shouldSend)
            PacketSender.SendPacket(packet, clientEp);
    }

    protected abstract bool Handle(T packet, IPEndPoint? clientEp);
}

public static class PacketSender
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPacket<T>(T packet) where T : IPacket
        => Main.Client.SendPacket(packet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPacket<T>(T packet, IPEndPoint? clientEp) where T : IPacket
        => Main.Server.SendToAllExcept(packet, clientEp);
}