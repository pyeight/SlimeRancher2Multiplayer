using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SR2MP.Server.Models;

namespace SR2MP.Server.Managers;

public class PlayerDataManager
{
    private readonly Dictionary<string, PlayerData> playerDataCache = new();
    private readonly string saveDirectory;
    // this needs to be something like: ORIGINAL-SR2-SAVE-ID_player_data.json
    private readonly string saveFileName = "player_data.json";
    private string SavePath => Path.Combine(saveDirectory, saveFileName);

    public event Action<PlayerData>? OnPlayerDataLoaded;
    public event Action<PlayerData>? OnPlayerDataSaved;

    public PlayerDataManager(string saveDirectory)
    {
        this.saveDirectory = saveDirectory;

        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
            SrLogger.LogMessage($"Created save directory: {saveDirectory}", SrLogger.LogTarget.Both);
        }

        LoadAllPlayerData();
    }

    public static string GeneratePersistentPlayerId()
    {
        try
        {
            // this works for linux too
            string systemInfo = $"{Environment.MachineName}" +
                                $"{Environment.UserName}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(systemInfo));

                string hash = BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .Substring(0, 9)
                    .ToUpper();

                string playerId = $"PLAYER_{hash}";

                SrLogger.LogMessage($"Generated persistent player ID: {playerId}", SrLogger.LogTarget.Both);
                return playerId;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to generate persistent player ID: {ex}", SrLogger.LogTarget.Both);
            return null;
        }
    }

    public PlayerData GetOrCreatePlayerData(string playerId, string playerName = "Undefined name")
    {
        if (playerDataCache.TryGetValue(playerId, out var existingData))
        {
            SrLogger.LogMessage($"Loaded existing data for player: {playerId}");
            OnPlayerDataLoaded?.Invoke(existingData);
            return existingData;
        }

        var newData = new PlayerData(playerId, playerName);
        playerDataCache[playerId] = newData;

        SrLogger.LogMessage($"Created new player data for: {playerId}", SrLogger.LogTarget.Both);
        SavePlayerData(newData);

        return newData;
    }

    // every time the player updates something qwq
    // ex. inventory, position + rotation, health + energy
    public void UpdatePlayerData(string playerId, Action<PlayerData> updateAction)
    {
        if (playerDataCache.TryGetValue(playerId, out var data))
        {
            updateAction(data);
            SavePlayerData(data);
        }
        else
        {
            SrLogger.LogWarning($"Attempted to update non-existent player data: {playerId}");
        }
    }

    public void SavePlayerData(PlayerData data)
    {
        try
        {
            playerDataCache[data.PlayerId] = data;
            SaveAllPlayerData();
            OnPlayerDataSaved?.Invoke(data);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to save player data for {data.PlayerId}: {ex}", SrLogger.LogTarget.Both);
        }
    }

    // on Server Close
    public void SaveAllPlayerData()
    {
        try
        {
            var json = JsonConvert.SerializeObject(playerDataCache.Values.ToList(), Formatting.Indented);
            File.WriteAllText(SavePath, json);
            SrLogger.LogMessage($"Saved data for {playerDataCache.Count} players", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to save player data: {ex}", SrLogger.LogTarget.Both);
        }
    }

    // on Server start
    private void LoadAllPlayerData()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                SrLogger.LogMessage("No player data file found, creating new",  SrLogger.LogTarget.Both);
                return;
            }

            var json = File.ReadAllText(SavePath);
            var playerList = JsonConvert.DeserializeObject<List<PlayerData>>(json);

            if (playerList != null)
            {
                foreach (var data in playerList)
                {
                    playerDataCache[data.PlayerId] = data;
                }

                SrLogger.LogMessage($"Loaded data for {playerDataCache.Count} players", SrLogger.LogTarget.Both);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to load player data: {ex}", SrLogger.LogTarget.Both);
        }
    }

    public PlayerData? GetPlayerData(string playerId)
    {
        return playerDataCache.TryGetValue(playerId, out var data) ? data : null;
    }

    public bool HasPlayerData(string playerId)
    {
        return playerDataCache.ContainsKey(playerId);
    }

    public List<PlayerData> GetAllPlayerData()
    {
        return playerDataCache.Values.ToList();
    }

    public bool DeletePlayerData(string playerId)
    {
        if (playerDataCache.Remove(playerId))
        {
            SaveAllPlayerData();
            SrLogger.LogMessage($"Deleted player data: {playerId}", SrLogger.LogTarget.Both);
            return true;
        }
        return false;
    }
}