using LiteNetLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public sealed class CurrencyHandler : BasePacketHandler<CurrencyPacket>
{
    public CurrencyHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(CurrencyPacket packet, NetPeer clientPeer)
    {
        var currency = GameContext.Instance.LookupDirector._currencyList._currencies[packet.CurrencyType - 1];

        var currencyDefinition = currency!.Cast<ICurrency>();

        var difference = packet.NewAmount - SceneContext.Instance.PlayerState.GetCurrency(currencyDefinition);

        handlingPacket = true;
        if (difference < 0)
            SceneContext.Instance.PlayerState.SpendCurrency(currencyDefinition, -difference);
        else
            SceneContext.Instance.PlayerState.AddCurrency(currencyDefinition, difference, packet.ShowUINotification);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}