using System.Net;
using SR2MP.Api;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Api;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Api;

[PacketHandler((byte)PacketType.ApiCall)]
internal sealed class ApiHandler : BasePacketHandler<ApiPacket>
{
    protected override bool Handle(ApiPacket packet, IPEndPoint? clientEp) => false;

    public override void Handle(PacketReader reader)
    {
        if (IsServerSide)
            return;

        if (!TryResolve(reader, false, out var apiPacket, out var packetSubType, out var holder))
            return;

        if (!holder.ClientHandlers.TryGetValue(packetSubType, out var handler))
            SrLogger.LogWarning($"No client API handler found for ModId {apiPacket.ModId}, packet subtype {packetSubType}.");
        else
            handler.Handle(reader);
    }

    public override void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (!IsServerSide)
            return;

        if (!TryResolve(reader, true, out var apiPacket, out var packetSubType, out var holder))
            return;

        if (!holder.ServerHandlers.TryGetValue(packetSubType, out var handler))
            SrLogger.LogWarning($"No server API handler found for ModId {apiPacket.ModId}, packet subtype {packetSubType}.");
        else
            handler.Handle(reader, clientEp);
    }

    private static bool TryResolve(PacketReader reader, bool isServerSide, out ApiPacket apiPacket, out byte packetSubType, out ApiHolder holder)
    {
        apiPacket = reader.ReadPacket<ApiPacket>();
        packetSubType = reader.ReadByte();

        if (ApiHandlers.Holders.TryGetValue(apiPacket.ModId, out holder!))
            return true;

        var side = isServerSide ? "server" : "client";
        SrLogger.LogWarning($"No API holder registered for ModId {apiPacket.ModId} ({side}-side).");
        return false;
    }
}