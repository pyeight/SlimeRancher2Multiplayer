using System.Net;
using SR2MP.Api;
using SR2MP.Packets;
using SR2MP.Packets.Internal;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.ModSync, HandlerType.Client)]
internal sealed class ModSyncHandler : BasePacketHandler<EmptyPacket>
{
    protected override bool Handle(EmptyPacket packet, IPEndPoint? clientEp)
    {
        var dict = new Dictionary<uint, ModData>();

        foreach (var id in ApiHandlers.SharedSideMods)
        {
            var info = ApiHandlers.Holders[id].Mod.Info;
            dict[id] = new ModData
            {
                Name = string.IsNullOrEmpty(info.Name) ? $"Unknown Mod ({id})" : info.Name,
                Version = string.IsNullOrEmpty(info.Version) ? "Unknown Version" : info.Version
            };
        }

        var modSyncPacket = new ModSyncPacket
        {
            PlayerId = LocalID,
            Mods = dict
        };
        Main.Client.SendPacket(modSyncPacket);

        return false;
    }
}