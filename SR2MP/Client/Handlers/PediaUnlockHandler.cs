using SR2MP.Packets.Pedia;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class PediaUnlockHandler : BaseClientPacketHandler<PediaUnlockPacket>
{
    public PediaUnlockHandler(Client client, RemotePlayerManager playerManager)
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