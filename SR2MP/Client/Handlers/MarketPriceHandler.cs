using SR2MP.Packets.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.MarketPriceChange)]
public sealed class MarketPriceHandler : BaseClientPacketHandler
{
    public MarketPriceHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<MarketPricePacket>();

        var economy = SceneContext.Instance.PlortEconomyDirector;

        //economy.ResetPrices(SceneContext.Instance.GameModel.world, 0);

        //SrLogger.LogMessage($"Market price change received!\nRecieved {packet.Prices.Length} prices.\nPrices:\n{string.Join(",\n", packet.Prices)}");

        int i = 0;
        // Couldn't do _currValueMap._values because its null for some reason, and
        // _currValueMap.Values is bugged with rider.
        foreach (var price in economy._currValueMap._entries)
        {
            if (price.value != null)
            {
                price.value.CurrValue = packet.Prices[i].Current;
                price.value.PrevValue = packet.Prices[i].Previous;
            }

            i++;
        }

        marketUIInstance?.EconUpdate();
    }
}