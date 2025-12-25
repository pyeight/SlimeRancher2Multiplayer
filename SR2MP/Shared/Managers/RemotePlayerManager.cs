using System.Collections.Concurrent;
using SR2MP.Client.Models;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Managers;

public class RemotePlayerManager
{
    private readonly ConcurrentDictionary<string, RemotePlayer> players = new();

    public event Action<string>? OnPlayerAdded;
    public event Action<string>? OnPlayerRemoved;
    public event Action<string, RemotePlayer>? OnPlayerUpdated;

    public int PlayerCount => players.Count;

    public RemotePlayer? GetPlayer(string playerId)
    {
        players.TryGetValue(playerId, out var player);
        return player;
    }

    public RemotePlayer AddPlayer(string playerId)
    {
        var player = new RemotePlayer(playerId);

        if (players.TryAdd(playerId, player))
        {
            SrLogger.LogMessage($"Remote player added: {playerId}", SrLogger.LogTarget.Both);
            OnPlayerAdded?.Invoke(playerId);
            return player;
        }
        else
        {
            SrLogger.LogWarning($"Remote player already exists: {playerId}", SrLogger.LogTarget.Both);
            return players[playerId];
        }
    }

    public bool RemovePlayer(string playerId)
    {
        if (players.TryRemove(playerId, out var player))
        {
            SrLogger.LogMessage($"Remote player removed: {playerId}", SrLogger.LogTarget.Both);
            OnPlayerRemoved?.Invoke(playerId);
            return true;
        }
        return false;
    }
    public void SendPlayerUpdate(
        UnityEngine.Vector3 position,
        float rotation,
        float horizontalMovement = 0f,
        float forwardMovement = 0f,
        float yaw = 0f,
        int airborneState = 0,
        bool moving = false,
        float horizontalSpeed = 0f,
        float forwardSpeed = 0f,
        bool sprinting = false,
        float lookY = 0f)
    {
        // I dont know.
        var playerId = Main.Client.IsConnected ? Main.Client.OwnPlayerId : Main.Server.IsRunning() ? "HOST" : "INVALID";
        var updatePacket = new PlayerUpdatePacket
        {
            Type = (byte)PacketType.PlayerUpdate,
            PlayerId = playerId,
            Position = position,
            Rotation = rotation,
            HorizontalMovement = horizontalMovement,
            ForwardMovement = forwardMovement,
            Yaw = yaw,
            AirborneState = airborneState,
            Moving = moving,
            HorizontalSpeed = horizontalSpeed,
            ForwardSpeed = forwardSpeed,
            Sprinting = sprinting,
            LookY = lookY
        };
        Main.SendToAllOrServer(updatePacket);
    }
    
    public void UpdatePlayer(
        string playerId,
        Vector3 position,
        float rotation,
        float horizontalMovement,
        float forwardMovement,
        float yaw,
        int airborneState,
        bool moving,
        float horizontalSpeed,
        float forwardSpeed,
        bool sprinting,
        float lookY)
    {
        if (players.TryGetValue(playerId, out var player))
        {
            player.Position = position;
            player.Rotation = rotation;
            player.HorizontalMovement = horizontalMovement;
            player.ForwardMovement = forwardMovement;
            player.Yaw = yaw;
            player.AirborneState = airborneState;
            player.Moving = moving;
            player.HorizontalSpeed = horizontalSpeed;
            player.ForwardSpeed = forwardSpeed;
            player.Sprinting = sprinting;
            player.LastLookY = player.LookY;
            player.LookY = lookY;
            player.LastUpdate = DateTime.UtcNow;
            OnPlayerUpdated?.Invoke(playerId, player);
        }
    }

    public List<RemotePlayer> GetAllPlayers()
    {
        return players.Values.ToList();
    }

    public void Clear()
    {
        var allPlayers = players.Keys.ToList();
        players.Clear();

        foreach (var playerId in allPlayers)
        {
            OnPlayerRemoved?.Invoke(playerId);
        }

        SrLogger.LogMessage("All remote players cleared!", SrLogger.LogTarget.Both);
    }
}