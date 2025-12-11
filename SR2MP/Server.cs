using SR2MP.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.S2C;
using SR2MP.Models;
using System.Net;

namespace SR2MP;

public sealed class Server
{
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;
    private readonly PacketManager packetManager;

    private Timer? timeoutTimer;

    public Server()
    {
        networkManager = new NetworkManager();
        clientManager = new ClientManager();
        packetManager = new PacketManager(networkManager, clientManager);

        networkManager.OnDataReceived += OnDataReceived;
        clientManager.OnClientRemoved += OnClientRemoved;
    }

    public void Start(int port)
    {
        if (networkManager.IsRunning)
        {
            SrLogger.LogMessageBoth("Server is already running!");
            return;
        }

        try
        {
            packetManager.RegisterHandlers();
            networkManager.Start(port);

            timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            SrLogger.LogMessageBoth($"Server started successfully on port {port}");
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorBoth($"Failed to start server: {ex}");
        }
    }

    public int GetClientCount() => clientManager.ClientCount;

    public bool IsRunning() => networkManager.IsRunning;

    private void OnDataReceived(byte[] data, IPEndPoint clientEP)
    {
        packetManager.HandlePacket(data, clientEP);
    }

    private void OnClientRemoved(ClientInfo client)
    {
        var leavePacket = new BroadcastPlayerLeavePacket
        {
            // This needs to be dynamic in the future
            Type = 4,
            PlayerId = client.PlayerId
        };

        using var writer = new PacketWriter();
        leavePacket.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var otherClient in clientManager.GetAllClients())
        {
            networkManager.Send(data, otherClient.EndPoint);
        }

        SrLogger.LogMessageBoth($"Player left broadcast sent for: {client.PlayerId}");
    }

    private void CheckTimeouts(object? state)
    {
        try
        {
            clientManager.RemoveTimedOutClients();
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorBoth($"Error checking timeouts: {ex}");
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
                // This needs to be dynamic in the future
                Type = 2
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
                    SrLogger.LogWarningSensitive($"Failed to send close packet to client: {client.GetClientInfo()}: {ex}");
                    SrLogger.LogWarning($"Failed to notify specific client of server shutdown: {ex}");
                }
            }

            clientManager.Clear();
            networkManager.Stop();

            SrLogger.LogMessageBoth("Server closed");
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorBoth($"Error during server shutdown: {ex}");
        }
    }
}