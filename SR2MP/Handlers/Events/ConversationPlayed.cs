using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Events;

[PacketHandler((byte)PacketType.ConversationPlayed)]
internal sealed class ConversationPlayedHandler : BasePacketHandler<ConversationPlayedPacket>
{
    protected override bool Handle(ConversationPlayedPacket packet, IPEndPoint? _)
    {
        NetworkConversationManager.ApplyConversationPlayed(packet.ConversationName);
        return true;
    }
}
