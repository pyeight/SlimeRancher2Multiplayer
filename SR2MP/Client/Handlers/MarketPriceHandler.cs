using SR2MP.Packets.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.MarketPriceChange)]
public sealed class MarketPriceHandler : BaseClientPacketHandler<MarketPricePacket>
{
    public MarketPriceHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(MarketPricePacket packet)
    {
        var economy = SceneContext.Instance.PlortEconomyDirector;

        //economy.ResetPrices(SceneContext.Instance.GameModel.world, 0);

        //SrLogger.LogMessage($"Market price change received!\nReceived {packet.Prices.Length} prices.\nPrices:\n{string.Join(",\n", packet.Prices)}");

        var i = 0;
        // Couldn't do _currValueMap._values because its null for some reason, and
        // _currValueMap.Values is bugged with rider.
        foreach (var price in economy._currValueMap._entries)
        {
            if (price.value != null)
            {
                (price.value.CurrValue, price.value.PrevValue) = packet.Prices[i];
            }

            i++;
        }

        marketUIInstance?.EconUpdate();
    }
}