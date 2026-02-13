using System.Net;
using SR2MP.Packets.Pedia;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.SlimePedia;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class SlimePediaUnlockHandler : BasePacketHandler<PediaUnlockPacket>
{
    public SlimePediaUnlockHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(PediaUnlockPacket packet, IPEndPoint senderEndPoint)
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