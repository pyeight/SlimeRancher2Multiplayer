using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Pedia;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.PediaUnlock)]
internal sealed class SlimePediaUnlockHandler : BasePacketHandler<PediaUnlockPacket>
{
    protected override bool Handle(PediaUnlockPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;
        SceneContext.Instance.PediaDirector.Unlock(
            GameContext.Instance.AutoSaveDirector
                ._saveReferenceTranslation._pediaEntryLookup[packet.ID],
            packet.Popup);
        HandlingPacket = false;

        return true;
    }
}