using System.Security.Cryptography;
using System.Text;

namespace SR2MP.Packets.Utils;

public static class PlayerIdGenerator
{
    public static string GeneratePersistentPlayerId()
    {
        try
        {
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

                SrLogger.LogMessage($"Generated persistent player ID: {playerId}");
                return playerId;
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to generate persistent player ID: {ex}", SrLogger.LogTarget.Both);
            return null;
        }
    }

    public static bool IsValidPlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        if (!playerId.StartsWith("PLAYER_"))
            return false;

        if (playerId.Length != 16)
            return false;

        return true;
    }
}