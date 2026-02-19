using SR2MP.Packets.Utils;
using System.Net;
using System.Runtime.CompilerServices;

namespace SR2MP.Shared.Handlers.Internal;

public abstract class BasePacketHandler<T> : IClientPacketHandler, IServerPacketHandler where T : IPacket, new()
{
    public bool IsServerSide { protected get; set; }

    public void Handle(PacketReader reader)
    {
        if (!IsServerSide)
            ProcessPacket(reader, null);
    }

    public void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (IsServerSide)
            ProcessPacket(reader, clientEp);
    }

    private void ProcessPacket(PacketReader reader, IPEndPoint? clientEp)
    {
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