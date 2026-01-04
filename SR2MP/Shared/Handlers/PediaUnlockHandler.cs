using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PediaUnlock)]
public sealed class PediaUnlockHandler : BaseSharedPacketHandler
{
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PediaUnlockPacket>();

        handlingPacket = true;
        SceneContext.Instance.PediaDirector.Unlock(
            GameContext.Instance.AutoSaveDirector
                ._saveReferenceTranslation._pediaEntryLookup[packet.ID],
            packet.Popup);
        handlingPacket = false;
        
        
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}