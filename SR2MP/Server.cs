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
            Logger.LogMessage("Server is already running!", Logger.LogTarget.Both);
            return;
        }

        try
        {
            packetManager.RegisterHandlers();
            networkManager.Start(port);

            timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            Logger.LogMessage($"Server started successfully on port {port}", Logger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start server: {ex}", Logger.LogTarget.Both);
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

        Logger.LogMessage($"Player left broadcast sent for: {client.PlayerId}", Logger.LogTarget.Both);
    }

    private void CheckTimeouts(object? state)
    {
        try
        {
            clientManager.RemoveTimedOutClients();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error checking timeouts: {ex}", Logger.LogTarget.Both);
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
                    Logger.LogWarning($"Failed to notify specific client of server shutdown: {ex}", $"Failed to send close packet to client: {client.GetClientInfo()}: {ex}");
                }
            }

            clientManager.Clear();
            networkManager.Stop();

            Logger.LogMessage("Server closed", Logger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error during server shutdown: {ex}", Logger.LogTarget.Both);
        }
    }
}