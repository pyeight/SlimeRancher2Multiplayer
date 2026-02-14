using System.Net;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Currency;

[PacketHandler((byte)PacketType.MarketPriceChange, HandlerType.Client)]
public sealed class MarketPriceHandler : BasePacketHandler<MarketPricePacket>
{
    protected override bool Handle(MarketPricePacket packet, IPEndPoint? _)
    {
        var economy = SceneContext.Instance.PlortEconomyDirector;
        var i = 0;

        foreach (var price in economy._currValueMap._entries)
        {
            if (price.value != null)
                (price.value.CurrValue, price.value.PrevValue) = packet.Prices[i];

            i++;
        }

        marketUIInstance?.EconUpdate();
        return false;
    }
}