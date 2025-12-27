using System.Net;

namespace SR2MP.Server.Models;

public sealed class ClientInfo
{
    public IPEndPoint EndPoint { get; set; }
    private DateTime LastHeartbeat { get; set; }
    public string PlayerId { get; set; }
    // Not sure if this is necessary
    // public string isAuthenticated { get; set; }

    public ClientInfo(IPEndPoint endPoint, string playerId = "")
    {
        EndPoint = endPoint;
        LastHeartbeat = DateTime.UtcNow;
        PlayerId = playerId;
        // Authenticated = isAuthenticated;
    }

    public void UpdateHeartbeat() => LastHeartbeat = DateTime.UtcNow;

    public bool IsTimedOut()
        => (DateTime.UtcNow - LastHeartbeat).TotalSeconds > 30;

    public string GetClientInfo() => $"{EndPoint.Address}:{EndPoint.Port}";
}