using System.Security.Cryptography;
using System.Text;

namespace SR2MP.Shared.Utils;

public static class PlayerIdGenerator
{
    public static string GeneratePersistentPlayerId()
    {
        try
        {
            var systemInfo = $"{Environment.MachineName}{Environment.UserName}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(systemInfo));

            var hash = BitConverter.ToString(hashBytes)
                .Replace("-", string.Empty)[..9]
                .ToUpper();

            var playerId = $"PLAYER_{hash}";

            SrLogger.LogMessage($"Generated persistent player ID: {playerId}", SrLogTarget.Both);
            return playerId;
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to generate persistent player ID: {ex}", SrLogTarget.Both);
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

    public static bool IsValidPlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        if (!playerId.StartsWith("PLAYER_"))
            return false;

        return playerId.Length == 16;
    }
}