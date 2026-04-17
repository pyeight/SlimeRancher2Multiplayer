using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Switch;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Access;

[PacketHandler((byte)PacketType.SwitchActivate)]
internal sealed class WorldSwitchHandler : BasePacketHandler<WorldSwitchPacket>
{
    protected override bool Handle(WorldSwitchPacket packet, IPEndPoint? _)
    {
        var gameModel = GameState;

        if (gameModel.switches.TryGetValue(packet.ID, out var switchModel))
        {
            switchModel.state = packet.State;

            if (switchModel.gameObj)
            {
                var switchComponentBase = switchModel.gameObj.GetComponent<WorldSwitchModel.Participant>();

                var primary = switchComponentBase.TryCast<WorldStatePrimarySwitch>();
                var secondary = switchComponentBase.TryCast<WorldStateSecondarySwitch>();
                var invisible = switchComponentBase.TryCast<WorldStateInvisibleSwitch>();

                HandlingPacket = true;
                primary?.SetStateForAll(packet.State, packet.Immediate);
                secondary?.SetState(packet.State, packet.Immediate);
                invisible?.SetStateForAll(packet.State, packet.Immediate);
                HandlingPacket = false;
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

        return true;
    }
}