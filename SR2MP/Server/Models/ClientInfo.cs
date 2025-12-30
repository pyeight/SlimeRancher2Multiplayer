using System.Net;

namespace SR2MP.Server.Models;

public sealed class ClientInfo
{
    public string Identifier { get; set; }
    public IPEndPoint EndPoint { get; set; }
    private DateTime LastHeartbeat { get; set; }
    public string PlayerId { get; set; }

    public ClientInfo(string identifier, IPEndPoint endPoint, string playerId = "")
    {
        Identifier = identifier;
        EndPoint = endPoint;
        LastHeartbeat = DateTime.UtcNow;
        PlayerId = playerId;
    }

    public void UpdateHeartbeat() => LastHeartbeat = DateTime.UtcNow;

    public bool IsTimedOut()
        => (DateTime.UtcNow - LastHeartbeat).TotalSeconds > 30;

    public string GetClientInfo() => Identifier;
}