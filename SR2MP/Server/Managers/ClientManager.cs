using System.Collections.Concurrent;
using LiteNetLib;
using SR2MP.Server.Models;

namespace SR2MP.Server.Managers;

public sealed class ClientManager
{
    private readonly ConcurrentDictionary<int, ClientInfo> clients = new();

    public event Action<ClientInfo>? OnClientAdded;
    public event Action<ClientInfo>? OnClientRemoved;

    public int ClientCount => clients.Count;

    public bool TryGetClient(NetPeer peer, out ClientInfo? client)
    {
        return clients.TryGetValue(peer.Id, out client);
    }

    public ClientInfo? GetClient(NetPeer peer)
    {
        clients.TryGetValue(peer.Id, out var client);
        return client;
    }

    public ClientInfo AddClient(NetPeer peer, string playerId)
    {
        var client = new ClientInfo(peer, playerId);

        if (clients.TryAdd(peer.Id, client))
        {
            SrLogger.LogMessage($"Client added! (PlayerId: {playerId})",
                $"Client added: {client.GetClientInfo()} (PlayerId: {playerId})");
            OnClientAdded?.Invoke(client);
            return client;
        }

        SrLogger.LogWarning($"Client already exists! (PlayerId: {playerId})",
            $"Client already exists: {client.GetClientInfo()} (PlayerId: {playerId})");
        return clients[peer.Id];
    }

    public bool RemoveClient(NetPeer peer)
    {
        if (clients.TryRemove(peer.Id, out var client))
        {
            SrLogger.LogMessage("Client removed!",
                $"Client removed: {client.GetClientInfo()}");
            OnClientRemoved?.Invoke(client);
            return true;
        }
        return false;
    }

    public ICollection<ClientInfo> GetAllClients() => clients.Values;

    public void Clear()
    {
        var allClients = clients.Values.ToList();
        clients.Clear();

        foreach (var client in allClients)
        {
            OnClientRemoved?.Invoke(client);
        }

        SrLogger.LogMessage("All clients cleared", SrLogTarget.Both);
    }
}
