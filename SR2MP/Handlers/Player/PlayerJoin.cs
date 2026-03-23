using System.Net;
using SR2MP.Components.Player;
using SR2MP.Components.UI;
using SR2MP.Handlers.Internal;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

public abstract class BasePlayerJoinHandler : BasePacketHandler<PlayerJoinPacket>
{
    protected static void InstantiatePlayer(PlayerJoinPacket packet)
    {
        var playerObject = Object.Instantiate(playerPrefab).GetComponent<NetworkPlayer>();
        playerObject.gameObject.SetActive(true);
        playerObject.ID = packet.PlayerId;
        playerObject.gameObject.name = packet.PlayerId;
        playerObjects.Add(packet.PlayerId, playerObject.gameObject);
        playerManager.AddPlayer(packet.PlayerId).Username = packet.PlayerName!;
        Object.DontDestroyOnLoad(playerObject);
    }
}

[PacketHandler((byte)PacketType.BroadcastPlayerJoin, HandlerType.Client)]
public sealed class ClientPlayerJoinHandler : BasePlayerJoinHandler
{
    protected override bool Handle(PlayerJoinPacket packet, IPEndPoint? _)
    {
        if (playerManager.GetPlayer(packet.PlayerId) != null)
        {
            SrLogger.LogPacketSize($"Player {packet.PlayerId} already exists");
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
public sealed class ServerPlayerJoinHandler : BasePlayerJoinHandler
{
    protected override bool Handle(PlayerJoinPacket packet, IPEndPoint? clientEp)
    {
        if (playerManager.GetPlayer(packet.PlayerId) != null)
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
        Main.Server.SendToAllExcept(joinChatPacket, packet.PlayerId);
        MultiplayerUI.Instance.RegisterSystemMessage($"{packet.PlayerName} joined the world!", $"SYSTEM_JOIN_HOST_{packet.PlayerId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageConnect);

        return false;
    }
}