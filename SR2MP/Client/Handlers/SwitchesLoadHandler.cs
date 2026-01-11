using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialSwitches)]
public sealed class SwitchesLoadHandler : BaseClientPacketHandler
{
    public SwitchesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<SwitchesPacket>();

        var gameModel = SceneContext.Instance.GameModel;

        foreach (var worldSwitch in packet.Switches)
        {
            if (gameModel.switches.TryGetValue(worldSwitch.ID, out var switchModel))
            {
                switchModel.state = worldSwitch.State;

                if (switchModel.gameObj)
                {
                    var switchComponentBase = switchModel.gameObj.GetComponent<WorldSwitchModel.Participant>();

                    switchComponentBase.SetModel(switchModel);

                    //var primary = switchComponentBase.TryCast<WorldStatePrimarySwitch>();
                    //var secondary = switchComponentBase.TryCast<WorldStateSecondarySwitch>();
                    //var invisible = switchComponentBase.TryCast<WorldStateInvisibleSwitch>();
                    //
                    //primary?.SetStateForAll();
                }
            }
            else
            {
                switchModel = new WorldSwitchModel
                {
                    gameObj = null,
                    state = worldSwitch.State,
                };

                gameModel.switches.Add(worldSwitch.ID, switchModel);
            }
        }
    }
}