using System.Collections.Concurrent;
using System.Net;
using SR2MP.Server.Models;

namespace SR2MP.Server.Managers;

public sealed class ClientManager
{
    private readonly ConcurrentDictionary<string, ClientInfo> clients = new();

    public event Action<ClientInfo>? OnClientAdded;
    public event Action<ClientInfo>? OnClientRemoved;
    public int ClientCount => clients.Count;

    public bool TryGetClient(string clientInfo, out ClientInfo? client)
    {
        return clients.TryGetValue(clientInfo, out client);
    }

    public bool TryGetClient(IPEndPoint endPoint, out ClientInfo? client)
    {
        string clientInfo = $"{endPoint.Address}:{endPoint.Port}";
        return TryGetClient(clientInfo, out client);
    }

    public ClientInfo? GetClient(string clientInfo)
    {
        clients.TryGetValue(clientInfo, out var client);
        return client;
    }

    public ClientInfo AddClient(IPEndPoint endPoint, string playerId)
    {
        string clientInfo = $"{endPoint.Address}:{endPoint.Port}";

        var client = new ClientInfo(endPoint, playerId);

        if (clients.TryAdd(clientInfo, client))
        {
            SrLogger.LogMessage($"Client added! (PlayerId: {playerId})",
                $"Client added: {clientInfo} (PlayerId: {playerId})");
            OnClientAdded?.Invoke(client);
            return client;
        }
        else
        {
            SrLogger.LogWarning($"Client already exists! (PlayerId: {playerId})",
                $"Client already exists: {clientInfo} (PlayerId: {playerId})");
            return clients[clientInfo];
        }
    }

    public bool RemoveClient(string clientInfo)
    {
        if (clients.TryRemove(clientInfo, out var client))
        {
            SrLogger.LogMessage($"Client removed!",
                $"Client removed: {clientInfo}");
            OnClientRemoved?.Invoke(client);
            return true;
        }
        return false;
    }

    public bool RemoveClient(IPEndPoint endPoint)
    {
        string clientInfo = $"{endPoint.Address}:{endPoint.Port}";
        return RemoveClient(clientInfo);
    }

    public void UpdateHeartbeat(string clientInfo)
    {
        if (clients.TryGetValue(clientInfo, out var client))
        {
            client.UpdateHeartbeat();
        }
    }

    public List<ClientInfo> GetAllClients()
    {
        return clients.Values.ToList();
    }

    public List<ClientInfo> GetTimedOutClients()
    {
        return clients.Values
            .Where(client => client.IsTimedOut())
            .ToList();
    }

    public void RemoveTimedOutClients()
    {
        var timedOut = GetTimedOutClients();
        foreach (var client in timedOut)
        {
            RemoveClient(client.GetClientInfo());
        }
    }

    public void Clear()
    {
        var allClients = clients.Values.ToList();
        clients.Clear();

        foreach (var client in allClients)
        {
            OnClientRemoved?.Invoke(client);
        }

        SrLogger.LogMessage("All clients cleared", SrLogger.LogTarget.Both);
    }
}