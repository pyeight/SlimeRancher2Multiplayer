using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Access;

[PacketHandler((byte)PacketType.InitialSwitches)]
public sealed class SwitchesLoadHandler : BaseClientPacketHandler<InitialSwitchesPacket>
{
    public SwitchesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialSwitchesPacket packet)
    {
        var gameModel = SceneContext.Instance.GameModel;

        foreach (var worldSwitch in packet.Switches)
        {
            if (gameModel.switches.TryGetValue(worldSwitch.ID, out var switchModel))
            {
                switchModel.state = worldSwitch.State;

                if (!switchModel.gameObj)
                    continue;
                var switchComponentBase = switchModel.gameObj.GetComponent<WorldSwitchModel.Participant>();

                switchComponentBase.SetModel(switchModel);

                var primary = switchComponentBase.TryCast<WorldStatePrimarySwitch>();
                var secondary = switchComponentBase.TryCast<WorldStateSecondarySwitch>();
                var invisible = switchComponentBase.TryCast<WorldStateInvisibleSwitch>();

                handlingPacket = true;
                primary?.SetStateForAll(worldSwitch.State, true);
                secondary?.SetState(worldSwitch.State, true);
                invisible?.SetStateForAll(worldSwitch.State, true);
                handlingPacket = false;
            }
            else
            {
                switchModel = new WorldSwitchModel
                {
                    gameObj = null,
                    state = worldSwitch.State
                };

                gameModel.switches.Add(worldSwitch.ID, switchModel);
            }
        }
    }
}