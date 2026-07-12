using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Upgrade;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Refinery;

[PacketHandler((byte)PacketType.ComponentAdd)]
internal sealed class UpgradeComponentUpdateHandler : BasePacketHandler<UpgradeComponentPacket>
{
    protected override bool Handle(UpgradeComponentPacket packet, IPEndPoint? _)
    {
        var model = GameState.PlayerModel.UpgradeComponentsModel;

        var comp = model._upgradeComponentList.items._items.FirstOrDefault(x => x._referenceId == packet.ComponentId);
        if (comp == null)
            return false;

        HandlingPacket = true;

        if (model._ownedComponents.ContainsKey(comp))
            model._ownedComponents[comp] = packet.Count;
        else
            model._ownedComponents.Add(comp, packet.Count);

        model.OwnedComponentsChanged?.Invoke(comp, packet.Count);
        model.NotifyParticipants();

        HandlingPacket = false;

        return true;
    }
}
