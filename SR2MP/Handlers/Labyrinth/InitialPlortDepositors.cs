using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPlortDepositors, HandlerType.Client)]
internal sealed class InitialPlortDepositorsHandler : BasePacketHandler<InitialPlortDepositorsPacket>
{
    protected override bool Handle(InitialPlortDepositorsPacket packet, IPEndPoint? _)
    {
        HandlingPacket = true;

        foreach (var depositor in packet.Depositors)
            NetworkDepositorManager.ApplyState(depositor.ID, depositor.AmountDeposited);

        HandlingPacket = false;

        return false;
    }
}
