using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Switch;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.Access;

[PacketHandler((byte)PacketType.SwitchActivate)]
public sealed class WorldSwitchHandler : BasePacketHandler<WorldSwitchPacket>
{
    public WorldSwitchHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(WorldSwitchPacket packet, IPEndPoint clientEp)
    {
        var gameModel = SceneContext.Instance.GameModel;

        if (gameModel.switches.TryGetValue(packet.ID, out var switchModel))
        {
            switchModel.state = packet.State;

            if (switchModel.gameObj)
            {
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

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}