using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Pedia;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class SlimePediaUnlockHandler : BaseClientPacketHandler<PediaUnlockPacket>
{
    public SlimePediaUnlockHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(PediaUnlockPacket packet)
    {
        handlingPacket = true;
        SceneContext.Instance.PediaDirector.Unlock(
            GameContext.Instance.AutoSaveDirector
                ._saveReferenceTranslation._pediaEntryLookup[packet.ID],
            packet.Popup);
        handlingPacket = false;
    }
}