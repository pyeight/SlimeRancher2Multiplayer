using System.Net;
using SR2MP.Packets;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.ModSync, HandlerType.Client)]
internal sealed class ModSyncHandler : BasePacketHandler<EmptyPacket>
{
    protected override bool Handle(EmptyPacket packet, IPEndPoint? clientEp)
    {
        var mods = Mods.ToDictionary(x => x.Hash(), x => (string?)x);
        var modSyncPacket = new ModSyncPacket() { Mods = mods };
        Main.Client.SendPacket(modSyncPacket);

        return false;
    }
}