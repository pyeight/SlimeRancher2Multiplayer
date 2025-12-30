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

    public bool TryGetClient(string clientIdentifier, out ClientInfo? client)
    {
        return clients.TryGetValue(clientIdentifier, out client);
    }

    public ClientInfo? GetClient(string clientIdentifier)
    {
        clients.TryGetValue(clientIdentifier, out var client);
        return client;
    }

    public ClientInfo AddClient(string clientIdentifier, IPEndPoint endPoint, string playerId)
    {
        var client = new ClientInfo(clientIdentifier, endPoint, playerId);

        if (clients.TryAdd(clientIdentifier, client))
        {
            SrLogger.LogMessage($"Client added! (PlayerId: {playerId})",
                $"Client added: {clientIdentifier} (PlayerId: {playerId})");
            OnClientAdded?.Invoke(client);
            return client;
        }
        else
        {
            SrLogger.LogWarning($"Client already exists! (PlayerId: {playerId})",
                $"Client already exists: {clientIdentifier} (PlayerId: {playerId})");
            return clients[clientIdentifier];
        }
    }

    public bool RemoveClient(string clientIdentifier)
    {
        if (clients.TryRemove(clientIdentifier, out var client))
        {
            SrLogger.LogMessage($"Client removed!",
                $"Client removed: {clientIdentifier}");
            OnClientRemoved?.Invoke(client);
            return true;
        }
        return false;
    }

    public void UpdateHeartbeat(string clientIdentifier)
    {
        if (clients.TryGetValue(clientIdentifier, out var client))
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
            RemoveClient(client.Identifier);
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
