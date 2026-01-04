using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.Close)]
public sealed class CloseHandler : BaseClientPacketHandler
{
    public CloseHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void HandleClient(byte[] data)
    {
        SrLogger.LogMessage("Server closed, disconnecting!", SrLogTarget.Both);

        Client.Disconnect();
    }
}