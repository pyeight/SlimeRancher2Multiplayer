using System.Net;
using SR2MP.Components.Player;
using SR2MP.Components.UI;
using SR2MP.Handlers.Internal;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

internal abstract class BasePlayerJoinHandler : BasePacketHandler<PlayerJoinPacket>
{
    protected static void InstantiatePlayer(PlayerJoinPacket packet)
    {
        var playerObject = Object.Instantiate(PlayerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = packet.PlayerId;
        playerObject.gameObject.name = packet.PlayerId;
        PlayerObjects.Add(packet.PlayerId, playerObject.gameObject);
        PlayerManager.AddPlayer(packet.PlayerId).Username = packet.PlayerName!;
        Object.DontDestroyOnLoad(playerObject);
    }
}

[PacketHandler((byte)PacketType.BroadcastPlayerJoin, HandlerType.Client)]
internal sealed class ClientPlayerJoinHandler : BasePlayerJoinHandler
{
    protected override bool Handle(PlayerJoinPacket packet, IPEndPoint? clientEp)
    {
        if (PlayerManager.GetPlayer(packet.PlayerId) != null)
        {
            SrLogger.LogDebug($"Player {packet.PlayerId} already exists");
            return false;
        }

        if (packet.PlayerId.Equals(Main.Client.PlayerId))
        {
            SrLogger.LogMessage("Player join request accepted!");
            return false;
        }

        SrLogger.LogMessage($"New Player joined! (PlayerId: {packet.PlayerId})");
        InstantiatePlayer(packet);
        return true;
    }
}

[PacketHandler((byte)PacketType.PlayerJoin, HandlerType.Server)]
internal sealed class ServerPlayerJoinHandler : BasePlayerJoinHandler
{
    protected override bool Handle(PlayerJoinPacket packet, IPEndPoint? clientEp)
    {
        if (PlayerManager.GetPlayer(packet.PlayerId) != null)
        {
            SrLogger.LogWarning($"Player {packet.PlayerId} already exists");
            return false;
        }

        var address = $"{clientEp!.Address}:{clientEp.Port}";
        SrLogger.LogMessage($"Player join request received (PlayerId: {packet.PlayerId})",
            $"Player join request from {address} (PlayerId: {packet.PlayerId})");

        InstantiatePlayer(packet);

        var joinPacket = new PlayerJoinPacket
        {
            Type = PacketType.BroadcastPlayerJoin,
            PlayerId = packet.PlayerId,
            PlayerName = packet.PlayerName
        };

        var joinChatPacket = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = $"{packet.PlayerName} joined the world!",
            MessageID = $"SYSTEM_JOIN_{packet.PlayerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageConnect
        };

        Main.Server.SendToAll(joinPacket);
        Main.Server.SendToAllExcept(joinChatPacket, clientEp);
        MultiplayerUI.Instance.RegisterSystemMessage($"{packet.PlayerName} joined the world!", $"SYSTEM_JOIN_HOST_{packet.PlayerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageConnect);

        return false;
    }
}