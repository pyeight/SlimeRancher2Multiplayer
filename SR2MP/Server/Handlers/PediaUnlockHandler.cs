using LiteNetLib;
using SR2MP.Packets.Pedia;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class PediaUnlockHandler : BasePacketHandler<PediaUnlockPacket>
{
    public PediaUnlockHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(PediaUnlockPacket packet, NetPeer senderEndPoint)
    {
        handlingPacket = true;
        SceneContext.Instance.PediaDirector.Unlock(
            GameContext.Instance.AutoSaveDirector
                ._saveReferenceTranslation._pediaEntryLookup[packet.ID],
            packet.Popup);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}