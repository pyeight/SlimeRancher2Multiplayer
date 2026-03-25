using System.Net;
using System.Runtime.CompilerServices;
using SR2MP.Packets.Utils;

namespace SR2MP.Api;

public abstract class BasePacketHandler<T> : IClientPacketHandler, IServerPacketHandler where T : ICustomPacket, new()
{
    public bool IsServerSide { protected get; set; }

    public void Handle(PacketReader reader)
    {
        if (!IsServerSide)
            ProcessPacket(reader, null);
    }

    public void Handle(PacketReader reader, IPEndPoint? selfEp)
    {
        if (IsServerSide)
            ProcessPacket(reader, selfEp);
    }

    private void ProcessPacket(PacketReader reader, IPEndPoint? selfEp)
    {
        var packet = reader.ReadCustomPacket<T>();
        var shouldSend = Handle(packet, selfEp);

        if (IsServerSide && shouldSend)
            PacketSender.SendDataToAllExcept(packet, selfEp);
    }

    protected abstract bool Handle(T packet, IPEndPoint? selfEp);
}

public static class PacketSender
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
    public static void SendData<T>(T packet) where T : ICustomPacket
        => Main.Client.SendData(packet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendDataToAllExcept<T>(T packet, IPEndPoint? excludeEndPoint) where T : ICustomPacket
        => Main.Server.SendDataToAllExcept(packet, excludeEndPoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
    public static void SendDataToClient<T>(T packet, IPEndPoint clientEp) where T : ICustomPacket
        => Main.Server.SendDataToClient(packet, clientEp);
}