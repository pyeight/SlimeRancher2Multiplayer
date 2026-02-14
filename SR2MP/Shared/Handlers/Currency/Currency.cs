using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Currency;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public sealed class CurrencyHandler : BasePacketHandler<CurrencyPacket>
{
    protected override bool Handle(CurrencyPacket packet, IPEndPoint? _)
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
        return true;
    }
}