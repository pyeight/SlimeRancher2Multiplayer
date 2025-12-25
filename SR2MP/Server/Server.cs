using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;

namespace SR2MP.Server;

public sealed class Server
{
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;
    private readonly PacketManager packetManager;
    private Timer? timeoutTimer;
    public int GetClientCount() => clientManager.ClientCount;
    public bool IsRunning() => networkManager.IsRunning;
    
    public event Action? OnServerStarted;
    
    public Server()
    {
        networkManager = new NetworkManager();
        clientManager = new ClientManager();
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
            // Commented because we don't need this yet
            // timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            OnServerStarted?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start server: {ex}", SrLogger.LogTarget.Both);
        }
    }

    private void OnDataReceived(byte[] data, System.Net.IPEndPoint clientEP)
    {
        SrLogger.LogPacketSize($"Received {data.Length} bytes from Client!",
            $"Received {data.Length} bytes from {clientEP}.");

        try
        {
            packetManager.HandlePacket(data, clientEP);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling packet from {clientEP}: {ex}", SrLogger.LogTarget.Both);
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
    
    
    public void SendToClient(IPacket packet, IPEndPoint endPoint)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        networkManager.Send(writer.ToArray(), endPoint);
    }

    public void SendToClient(IPacket packet, ClientInfo client)
    {
        SendToClient(packet, client.EndPoint);
    }

    public void SendToAll(IPacket packet)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            networkManager.Send(data, client.EndPoint);
        }
    }

    public void SendToAllExcept(IPacket packet, string excludedClientInfo)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            if (client.GetClientInfo() != excludedClientInfo)
            {
                networkManager.Send(data, client.EndPoint);
            }
        }
    }

    public void SendToAllExcept(IPacket packet, IPEndPoint excludeEndPoint)
    {
        string clientInfo = $"{excludeEndPoint.Address}:{excludeEndPoint.Port}";
        SendToAllExcept(packet, clientInfo);
    }
}