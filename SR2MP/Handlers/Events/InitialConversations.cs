using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Events;

[PacketHandler((byte)PacketType.InitialConversations, HandlerType.Client)]
internal sealed class InitialConversationsHandler : BasePacketHandler<InitialConversationsPacket>
{
    protected override bool Handle(InitialConversationsPacket packet, IPEndPoint? _)
    {
        foreach (var name in packet.ConversationNames)
        {
            if (name != null)
                NetworkConversationManager.ApplyConversationPlayed(name);
        }

        return false;
    }
}
