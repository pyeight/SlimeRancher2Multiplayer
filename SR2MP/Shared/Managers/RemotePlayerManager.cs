using System.Collections.Concurrent;
using SR2MP.Client.Models;
using SR2MP.Packets.Player;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public sealed class RemotePlayerManager
{
    private readonly ConcurrentDictionary<string, RemotePlayer> players = new();

    public event Action<string>? OnPlayerAdded;
    public event Action<string>? OnPlayerRemoved;
    public event Action<string, RemotePlayer>? OnPlayerUpdated;
    public event Action<string, RemotePlayer>? OnPlayerGadgetUpdated;

    public int PlayerCount => players.Count;

    public RemotePlayer? GetPlayer(string playerId)
    {
        players.TryGetValue(playerId, out var player);
        return player;
    }

    internal RemotePlayer AddPlayer(string playerId)
    {
        var player = new RemotePlayer(playerId);

        if (players.TryAdd(playerId, player))
        {
            SrLogger.LogMessage($"Remote player added: {playerId}");
            OnPlayerAdded?.Invoke(playerId);
            return player;
        }

        SrLogger.LogWarning($"Remote player already exists: {playerId}");
        return players[playerId];
    }

    internal bool RemovePlayer(string playerId)
    {
        if (!players.TryRemove(playerId, out _))
            return false;
        SrLogger.LogMessage($"Remote player removed: {playerId}");
        OnPlayerRemoved?.Invoke(playerId);
        return true;
    }

    internal static void SendPlayerUpdate(
        Vector3 position,
        float rotation,
        float horizontalMovement = 0f,
        float forwardMovement = 0f,
        float yaw = 0f,
        int airborneState = 0,
        bool moving = false,
        float horizontalSpeed = 0f,
        float forwardSpeed = 0f,
        bool sprinting = false,
        float lookY = 0f,
        int sceneGroup = 1)
    {
        var playerId = Main.Client.IsConnected ? Main.Client.PlayerId : (Main.Server.IsRunning ? Main.Server.PlayerId : string.Empty);
        var updatePacket = new PlayerUpdatePacket
        {
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
            LookY = lookY,
            SceneGroup = sceneGroup
        };
        Main.SendToAllOrServer(updatePacket);
    }

    internal void UpdatePlayer(
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
        float lookY,
        int sceneGroup)
    {
        if (!players.TryGetValue(playerId, out var player))
            return;

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
        player.SceneGroup = sceneGroup;

        OnPlayerUpdated?.Invoke(playerId, player);
    }
    
    internal void UpdatePlayerGadget(
        string playerId,
        bool enabled,
        int gadgetId = -1,
        bool valid = false,
        Vector3 position = default,
        Quaternion rotation = default,
        Quaternion localRotation = default)
    {
        if (!players.TryGetValue(playerId, out var player))
            return;

        player.OnlineGadgetMode = enabled;
        player.OnlineGadgetID = gadgetId;
        player.OnlineGadgetValid = valid;
        player.OnlineGadgetPosition = position;
        player.OnlineGadgetRotation = rotation;
        player.OnlineGadgetLocalRotation = localRotation;

        OnPlayerGadgetUpdated?.Invoke(playerId, player);
    }

    public List<RemotePlayer> GetAllPlayers()
    {
        return players.Values.ToList();
    }

    internal void Clear()
    {
        var allPlayers = players.Keys.ToList();
        players.Clear();

        foreach (var playerId in allPlayers)
            OnPlayerRemoved?.Invoke(playerId);

        SrLogger.LogMessage("All remote players cleared!");
    }

    /// <summary>
    /// Gets a player color that is good for ui. Each value in RGB has a minimum of 70.
    /// </summary>
    public static Color GetPlayerColor(RemotePlayer player)
    {
        var hash = player.PlayerId.Replace("PLAYER_", "").Hash32();
        Main.ModRandomization.Reseed((int)hash);
        var random = Main.ModRandomization;
        return new Color32(
            (byte)random.GetInRange(70, 255),
            (byte)random.GetInRange(70, 255),
            (byte)random.GetInRange(70, 255),
            255);
    }
}