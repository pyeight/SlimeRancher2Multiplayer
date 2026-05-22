using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Currency;

[PacketHandler((byte)PacketType.MarketPriceChange, HandlerType.Client)]
internal sealed class MarketPriceHandler : BasePacketHandler<MarketPricePacket>
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

        try
        {
            MarketUIInstance?.EconUpdate();
        } catch { /* ignored */ }
        return false;
    }
}