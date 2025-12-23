using System.Net;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
public class CurrencyHandler : BasePacketHandler
{
    public CurrencyHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
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
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}