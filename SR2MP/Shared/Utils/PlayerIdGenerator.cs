using System.Security.Cryptography;
using System.Text;

namespace SR2MP.Shared.Utils;

internal static class PlayerIdGenerator
{
    public static string GeneratePersistentPlayerId()
    {
        try
        {
            var hashBytes = DevMode
                ? RandomNumberGenerator.GetBytes(16)
                : SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserName));

            var hash = BitConverter.ToString(hashBytes)
                .Replace("-", string.Empty)[..9]
                .ToUpperInvariant();

            var playerId = $"PLAYER_{hash}";

            SrLogger.LogMessage($"Generated persistent player ID: {playerId}");
            return playerId;
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to generate persistent player ID: {ex}");
            return null!;
        }
    }

    public static ushort GetPlayerIDNumber(string id)
    {
        ushort number = 12345;
        foreach (var c in id[7..])
            number = (ushort)((number << 5) + number + c);
        return number;
    }

    // public static bool IsValidPlayerId(string playerId)
    // {
    //     if (string.IsNullOrWhiteSpace(playerId))
    //         return false;

    //     if (!playerId.StartsWith("PLAYER_"))
    //         return false;

    //     return playerId.Length == 16;
    // }
}