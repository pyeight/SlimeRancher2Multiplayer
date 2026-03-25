using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Internal;

[PacketHandler((byte)PacketType.Connect, HandlerType.Server)]
internal sealed class ConnectHandler : BasePacketHandler<ConnectPacket>
{
    protected override bool Handle(ConnectPacket packet, IPEndPoint? clientEp)
    {
        if (clientEp == null)
            return false;

        SrLogger.LogMessage(
            $"Connect request received with PlayerId: {packet.PlayerId}",
            $"Connect request from {clientEp} with PlayerId: {packet.PlayerId}");

        Main.Server.ClientManager.AddClient(clientEp, packet.PlayerId);

        var money = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>());
        var rainbowMoney = SceneContext.Instance.PlayerState.GetCurrency(
            GameContext.Instance.LookupDirector._currencyList[1].Cast<ICurrency>());

        var diff = false;

        var mods = Mods.ToList().ConvertAll(mod => mod.Hash());
        foreach (var mod in mods)
        {
            if (diff) break;
            if (!packet.ModHashes.Contains(mod))
            {
                diff = true;
                break;
            }
        }
        foreach (var mod in packet.ModHashes)
        {
            if (diff) break;
            if (!mods.Contains(mod))
            {
                diff = true;
                break;
            }
        }

        if (diff)
        {
            var informPacket = new EmptyPacket()
            {
                Type = PacketType.ModSync,
                Reliability = PacketReliability.ReliableOrdered
            };
            Main.Server.SendToClient(informPacket, clientEp);
            return false;
        }
        var ackPacket = new ConnectionApprovePacket
        {
            InitialJoin = true,
            PlayerId = packet.PlayerId,
            OtherPlayers = Array.ConvertAll(PlayerManager.GetAllPlayers().ToArray(),
                p => (p.PlayerId, p.Username)),
            Money = money,
            RainbowMoney = rainbowMoney,
            AllowCheats = Main.AllowCheats
        };

        // The connectAck is different because of initialJoin, otherwise another PlayerJoin request will be sent

        Main.Server.SendToClient(ackPacket, clientEp);

        ReSyncManager.SynchronizeClient(packet.PlayerId, clientEp);

        SrLogger.LogMessage(
            $"Player {packet.PlayerId} successfully connected",
            $"Player {packet.PlayerId} successfully connected from {clientEp}");

        return false;
    }
}