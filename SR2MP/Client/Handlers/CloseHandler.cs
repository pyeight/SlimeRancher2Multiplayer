using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.Close)]
public class CloseHandler : BaseClientPacketHandler
{
    public CloseHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        SrLogger.LogMessage("Server closed, disconnecting!", SrLogger.LogTarget.Both);

        Client.Disconnect();
    }
}