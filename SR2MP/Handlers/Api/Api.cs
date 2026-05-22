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

        HandlingPacket = true;

        if (!holder.ClientHandlers.TryGetValue(packetSubType, out var handler))
            SrLogger.LogWarning($"No client API handler found for ModId {apiPacket.NetId}, packet subtype {packetSubType}.");
        else
            handler.Handle(reader);

        HandlingPacket = false;
    }

    public override void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (!IsServerSide)
            return;

        if (!TryResolve(reader, true, out var apiPacket, out var packetSubType, out var holder))
            return;

        HandlingPacket = true;

        if (!holder.ServerHandlers.TryGetValue(packetSubType, out var handler))
            SrLogger.LogWarning($"No server API handler found for ModId {apiPacket.NetId}, packet subtype {packetSubType}.");
        else
            handler.Handle(reader, clientEp);

        HandlingPacket = false;
    }

    private static bool TryResolve(PacketReader reader, bool isServerSide, out ApiPacket apiPacket, out byte packetSubType, out ApiHolder holder)
    {
        apiPacket = reader.ReadPacket<ApiPacket>();
        packetSubType = reader.ReadByte();

        if (ApiHandlers.CurrentNetIdMappingReverse.TryGetValue(apiPacket.NetId, out var modId) && ApiHandlers.Holders.TryGetValue(modId, out holder!))
            return true;

        holder = null!;
        var side = isServerSide ? "server" : "client";
        SrLogger.LogWarning($"No API holder registered for ModId {apiPacket.NetId} ({side}-side).");
        return false;
    }
}