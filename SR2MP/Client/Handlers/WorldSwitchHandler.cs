using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Switch;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.SwitchActivate)]
public sealed class WorldSwitchHandler : BaseClientPacketHandler<WorldSwitchPacket>
{
    public WorldSwitchHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(WorldSwitchPacket packet)
    {
        var gameModel = SceneContext.Instance.GameModel;

        if (gameModel.switches.TryGetValue(packet.ID, out var switchModel))
        {
            switchModel.state = packet.State;

            if (!switchModel.gameObj)
                return;
            var switchComponentBase = switchModel.gameObj.GetComponent<WorldSwitchModel.Participant>();

            var primary = switchComponentBase.TryCast<WorldStatePrimarySwitch>();
            var secondary = switchComponentBase.TryCast<WorldStateSecondarySwitch>();
            var invisible = switchComponentBase.TryCast<WorldStateInvisibleSwitch>();

            handlingPacket = true;
            primary?.SetStateForAll(packet.State, packet.Immediate);
            secondary?.SetState(packet.State, packet.Immediate);
            invisible?.SetStateForAll(packet.State, packet.Immediate);
            handlingPacket = false;
        }
        else
        {
            switchModel = new WorldSwitchModel
            {
                gameObj = null,
                state = packet.State
            };

            gameModel.switches.Add(packet.ID, switchModel);
        }
    }
}