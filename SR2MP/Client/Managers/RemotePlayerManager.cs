using System.Collections.Concurrent;
using SR2MP.Client.Models;
using UnityEngine;

namespace SR2MP.Client.Managers;

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

    public void UpdatePlayerFull(
        string playerId,
        Vector3 position,
        Quaternion rotation,
        float horizontalMovement,
        float forwardMovement,
        float yaw,
        int airborneState,
        bool moving,
        float horizontalSpeed,
        float forwardSpeed,
        bool sprinting)
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