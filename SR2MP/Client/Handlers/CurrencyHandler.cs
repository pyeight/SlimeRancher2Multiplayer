using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public sealed class CurrencyHandler : BaseClientPacketHandler
{
    public CurrencyHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<CurrencyPacket>();

        var currency = GameContext.Instance.LookupDirector._currencyList._currencies[packet.CurrencyType - 1];

        var currencyDefinition = currency!.Cast<ICurrency>();

        var difference = packet.NewAmount - SceneContext.Instance.PlayerState.GetCurrency(currencyDefinition);
        
        handlingPacket = true;
        if (difference < 0)
            SceneContext.Instance.PlayerState.SpendCurrency(currencyDefinition, -difference);
        else
            SceneContext.Instance.PlayerState.AddCurrency(currencyDefinition, difference, packet.ShowUINotification);
        handlingPacket = false;
    }
}