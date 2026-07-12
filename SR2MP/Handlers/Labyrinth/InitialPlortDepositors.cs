using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPlortDepositors, HandlerType.Client)]
internal sealed class InitialPlortDepositorsHandler : BasePacketHandler<InitialPlortDepositorsPacket>
{
    protected override bool Handle(InitialPlortDepositorsPacket packet, IPEndPoint? _)
    {
        foreach (var depositor in packet.Depositors)
        {
            if (GameState.depositors.TryGetValue(depositor.ID, out var depositorModel))
            {
                depositorModel.AmountDeposited = depositor.AmountDeposited;
                if (depositorModel._gameObject)
                {
                    var depositorComponent = depositorModel._gameObject.GetComponent<PlortDepositor>();
                    if (depositorComponent)
                    {
                        depositorComponent.OnFilledChangedFromModel();
                    }
                }
            }
            else
            {
                depositorModel = new PlortDepositorModel
                {
                    _gameObject = null,
                    AmountDeposited = depositor.AmountDeposited
                };
                GameState.depositors.Add(depositor.ID, depositorModel);
            }
        }

        return false;
    }
}
