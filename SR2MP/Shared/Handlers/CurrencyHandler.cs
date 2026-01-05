using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public sealed class CurrencyHandler : BaseSharedPacketHandler
{
    public CurrencyHandler(NetworkManager networkManager, ClientManager clientManager) {}
    public CurrencyHandler(Client.Client client, RemotePlayerManager playerManager) {}
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<CurrencyPacket>();

        var currency = GameContext.Instance.LookupDirector._currencyList._currencies[packet.CurrencyType - 1];

        handlingPacket = true;
        if (packet.Adjust < 0)
            SceneContext.Instance.PlayerState.SpendCurrency(currency!.Cast<ICurrency>(), -packet.Adjust);
        else
            SceneContext.Instance.PlayerState.AddCurrency(currency!.Cast<ICurrency>(), packet.Adjust, packet.ShowUINotification);
        handlingPacket = false;

       
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}