using SR2MP.Packets.Pedia;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class SlimePediaUnlockHandler : BasePacketHandler<PediaUnlockPacket>
{
    public SlimePediaUnlockHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(PediaUnlockPacket packet, IPEndPoint? _)
    {
        handlingPacket = true;
        SceneContext.Instance.PediaDirector.Unlock(
            GameContext.Instance.AutoSaveDirector
                ._saveReferenceTranslation._pediaEntryLookup[packet.ID],
            packet.Popup);
        handlingPacket = false;

        return true;
    }
}