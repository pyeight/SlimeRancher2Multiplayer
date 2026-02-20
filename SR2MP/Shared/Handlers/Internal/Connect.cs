using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers.Internal;

[PacketHandler((byte)PacketType.Connect, HandlerType.Server)]
public sealed class ConnectHandler : BasePacketHandler<ConnectPacket>
{
    protected override bool Handle(ConnectPacket packet, IPEndPoint? clientEp)
    {
        if (clientEp == null)
            return false;

        SrLogger.LogMessage(
            $"Connect request received with PlayerId: {packet.PlayerId}",
            $"Connect request from {clientEp} with PlayerId: {packet.PlayerId}");

        Main.Server.clientManager.AddClient(clientEp, packet.PlayerId);
        
        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());
        
        var ackPacket = new ConnectAckPacket
        {
            initialJoin = true,
            PlayerId = packet.PlayerId,
            OtherPlayers = Array.ConvertAll(playerManager.GetAllPlayers().ToArray(),
                p => (p.PlayerId, p.Username)),
            Money = money,
            RainbowMoney = rainbowMoney,
            AllowCheats = Main.AllowCheats
        };
        
        // The connectAck is different because of initialJoin, otherwise another PlayerJoin request will be sent
        
        Main.Server.SendToClient(ackPacket, clientEp);
        
        Main.Server.reSyncManager.SynchronizeClient(packet.PlayerId, clientEp);

        SrLogger.LogMessage(
            $"Player {packet.PlayerId} successfully connected",
            $"Player {packet.PlayerId} successfully connected from {clientEp}");

        return false;
    }
}
