using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public class CurrencyHandler : BaseClientPacketHandler
{
    public CurrencyHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<CurrencyPacket>();

        var currency = GameContext.Instance.LookupDirector._currencyList._currencies[packet.CurrencyType - 1];
        
        handlingPacket = true;
        if (packet.Adjust < 0)
            SceneContext.Instance.PlayerState.SpendCurrency(currency!.Cast<ICurrency>(), -packet.Adjust, null);
        else
            SceneContext.Instance.PlayerState.AddCurrency(currency!.Cast<ICurrency>(), packet.Adjust, packet.ShowUINotification, null);
        handlingPacket = false;
    }
}