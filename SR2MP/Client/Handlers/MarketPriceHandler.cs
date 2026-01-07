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

        int i = 0;
        foreach (var price in SceneContext.Instance.PlortEconomyDirector._currValueMap._values)
        {
            price.CurrValue = packet.Prices[i];
            i++;
        }
    }
}