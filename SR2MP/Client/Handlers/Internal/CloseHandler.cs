using SR2MP.Packets;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Internal;

[PacketHandler((byte)PacketType.Close)]
public sealed class CloseHandler : BaseClientPacketHandler<ClosePacket>
{
    public CloseHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(ClosePacket packet)
    {
        SrLogger.LogMessage("Server closed, disconnecting!", SrLogTarget.Both);

        Client.Disconnect();
    }
}