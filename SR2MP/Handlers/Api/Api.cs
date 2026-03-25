using System.Net;
using SR2MP.Api;
using SR2MP.Packets.Api;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Api;

[PacketHandler((byte)PacketType.ApiCall)]
internal sealed class ApiHandler : Internal.BasePacketHandler<ApiPacket>
{
    protected override bool Handle(ApiPacket packet, IPEndPoint? clientEp) => false;

    public override void Handle(PacketReader reader)
    {
        if (IsServerSide)
            return;

        reader.ReadPacket<ApiPacket>();

        if (ApiHandlers.ClientHandlers.TryGetValue(reader.ReadByte(), out var handler))
            handler.Handle(reader);
    }

    public override void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (!IsServerSide)
            return;

        reader.ReadPacket<ApiPacket>();

        if (ApiHandlers.ServerHandlers.TryGetValue(reader.ReadByte(), out var handler))
            handler.Handle(reader, clientEp);
    }
}