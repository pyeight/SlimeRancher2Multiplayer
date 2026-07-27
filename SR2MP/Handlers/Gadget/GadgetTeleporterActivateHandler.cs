using System.Net;
using Il2CppMonomiPark.SlimeRancher.World.Teleportation;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Gadget;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Gadget;

[PacketHandler((byte)PacketType.GadgetTeleporterFX)]
internal sealed class GadgetTeleporterActivateHandler : BasePacketHandler<GadgetTeleporterActivatePacket>
{
    protected override bool Handle(GadgetTeleporterActivatePacket packet, IPEndPoint? _)
    {
        var obj = GameObject.Find(packet.ObjectPath);
        if (!obj)
            return true;

        var node = obj.GetComponent<GadgetTeleporterNode>();
        if (!node)
            return true;

        var fx = packet.IsArrival ? node._arriveFX : node._departFX;
        var sfx = packet.IsArrival ? node._arriveSfx : node._departSfx;

        HandlingPacket = true;

        if (fx)
        {
            try { FXHelpers.SpawnAndPlayFX(fx, node.transform.position, node.transform.rotation); }
            catch { /* ignored */ }
        }

        if (sfx)
            RemoteFXManager.PlayTransientAudio(sfx, node.transform.position, 1f);

        HandlingPacket = false;

        return true;
    }
}
