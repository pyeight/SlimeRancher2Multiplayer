using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Server;

public sealed class Server
{
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;
    public readonly PlayerInventoryManager playerInventoryManager; // Made public for handlers
    private readonly PacketManager packetManager;
    private Timer? timeoutTimer;
    public int GetClientCount() => clientManager.ClientCount;
    public bool IsRunning() => networkManager.IsRunning;

    public event Action? OnServerStarted;

    public Server()
    {
        networkManager = new NetworkManager();
        clientManager = new ClientManager();
        playerInventoryManager = new PlayerInventoryManager();
        packetManager = new PacketManager(networkManager, clientManager);

        networkManager.OnDataReceived += OnDataReceived;
        clientManager.OnClientRemoved += OnClientRemoved;
    }

    public void Start(int port, bool enableIPv6)
    {
        if (networkManager.IsRunning)
        {
            SrLogger.LogMessage("Server is already running!", SrLogger.LogTarget.Both);
            return;
        }

        try
        {
            packetManager.RegisterHandlers();
            Application.quitting += new System.Action(Close);
            networkManager.Start(port, enableIPv6);
            
            // timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            OnServerStarted?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start server: {ex}", SrLogger.LogTarget.Both);
        }
    }

    private void OnDataReceived(byte[] data, string clientIdentifier)
    {
        SrLogger.LogPacketSize($"Received {data.Length} bytes from Client!",
            $"Received {data.Length} bytes from {clientIdentifier}.");

        try
        {
            packetManager.HandlePacket(data, clientIdentifier);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling packet from {clientIdentifier}: {ex}", SrLogger.LogTarget.Both);
        }
    }

    private void OnClientRemoved(Models.ClientInfo client)
    {
        var leavePacket = new PlayerLeavePacket
        {
            Type = (byte)PacketType.BroadcastPlayerLeave,
            PlayerId = client.PlayerId
        };

        using var writer = new PacketWriter();
        leavePacket.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var otherClient in clientManager.GetAllClients())
        {
            networkManager.Send(data, otherClient.EndPoint);
        }

        SrLogger.LogMessage($"Player left broadcast sent for: {client.PlayerId}", SrLogger.LogTarget.Both);

        playerInventoryManager.RemovePlayer(client.PlayerId);
    }

    private void CheckTimeouts(object? state)
    {
        try
        {
            clientManager.RemoveTimedOutClients();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error checking timeouts: {ex}");
        }
    }

    public void Close()
    {
        if (!networkManager.IsRunning)
            return;

        try
        {
            timeoutTimer?.Dispose();
            timeoutTimer = null;

            var closePacket = new ClosePacket
            {
                Type = (byte)PacketType.Close
            };

            using var writer = new PacketWriter();
            closePacket.Serialise(writer);
            byte[] data = writer.ToArray();

            foreach (var client in clientManager.GetAllClients())
            {
                try
                {
                    networkManager.Send(data, client.EndPoint);
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Failed to notify specific client of server shutdown: {ex}",
                        $"Failed to send close packet to client: {client.GetClientInfo()}: {ex}");
                }
            }
            clientManager.Clear();
            networkManager.Stop();

            SrLogger.LogMessage("Server closed", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during server shutdown: {ex}", SrLogger.LogTarget.Both);
        }
    }


    public void SendToClient<T>(T packet, string clientIdentifier) where T : IPacket
    {
        if (clientManager.TryGetClient(clientIdentifier, out var client))
        {
            using var writer = new PacketWriter();
            packet.Serialise(writer);
            networkManager.Send(writer.ToArray(), client.EndPoint);
        }
    }

    public void SendToClient<T>(T packet, ClientInfo client) where T : IPacket
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        networkManager.Send(writer.ToArray(), client.EndPoint);
    }

    public void Send(byte[] data, string clientIdentifier)
    {
        if (clientManager.TryGetClient(clientIdentifier, out var client))
        {
            networkManager.Send(data, client.EndPoint);
        }
    }

    public void Send<T>(T packet, string clientIdentifier) where T : IPacket
    {
        SendToClient(packet, clientIdentifier);
    }

    public void SendToAll<T>(T packet) where T : IPacket
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            networkManager.Send(data, client.EndPoint);
        }
    }

    public void SendToAll(byte[] data)
    {
        foreach (var client in clientManager.GetAllClients())
        {
            networkManager.Send(data, client.EndPoint);
        }
    }

    public void SendToAllExcept<T>(T packet, string excludedClientIdentifier) where T : IPacket
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            if (client.Identifier != excludedClientIdentifier)
            {
                networkManager.Send(data, client.EndPoint);
            }
        }
    }

    public void SendToAllExcept(byte[] data, string excludedClientIdentifier)
    {
        foreach (var client in clientManager.GetAllClients())
        {
            if (client.Identifier != excludedClientIdentifier)
            {
                networkManager.Send(data, client.EndPoint);
            }
        }
    }
}