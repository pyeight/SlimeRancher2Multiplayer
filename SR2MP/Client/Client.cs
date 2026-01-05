using System.Net;
using System.Net.Sockets;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;
using Thread = Il2CppSystem.Threading.Thread;

namespace SR2MP.Client;

public sealed class Client
{
    private UdpClient? udpClient;
    private IPEndPoint? serverEndPoint;
    private Thread? receiveThread;
    private Timer? heartbeatTimer;

    private volatile bool isConnected;
    private volatile bool connectionAcknowledged;
    private Timer? connectionTimeoutTimer;
    private const int ConnectionTimeoutSeconds = 10;

    private readonly ClientPacketManager packetManager;

    public string OwnPlayerId { get; private set; } = string.Empty;
    public bool IsConnected => isConnected;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<string, RemotePlayer>? OnPlayerUpdate;
    public event Action<string, string, DateTime>? OnChatMessageReceived;

    public Client()
    {
        packetManager = new ClientPacketManager(this, playerManager);

        playerManager.OnPlayerAdded += playerId => OnPlayerJoined?.Invoke(playerId);
        playerManager.OnPlayerRemoved += playerId => OnPlayerLeft?.Invoke(playerId);
        playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    public void Connect(string serverIp, int port)
    {
        if (isConnected)
        {
            SrLogger.LogMessage("You are already connected to a Server!", SrLogTarget.Both);
            return;
        }

        try
        {
            IPAddress parsedIp = IPAddress.Parse(serverIp);

            if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (!Socket.OSSupportsIPv6)
                {
                    SrLogger.LogError("IPv6 is not supported on this machine! Please enable IPv6 or use an IPv4 address.", SrLogTarget.Both);
                    throw new NotSupportedException("IPv6 is not available on this system");
                }
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                SrLogger.LogMessage("Using IPv6 connection", SrLogTarget.Both);
            }
            else
            {
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                SrLogger.LogMessage("Using IPv4 connection", SrLogTarget.Both);
            }

            serverEndPoint = new IPEndPoint(parsedIp, port);
            udpClient.Connect(serverEndPoint);

            OwnPlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();

            packetManager.RegisterHandlers();

            isConnected = true;
            connectionAcknowledged = false;

            receiveThread = new Thread(new Action(ReceiveLoop))
            {
                IsBackground = true
            };
            receiveThread.Start();
            
            connectionTimeoutTimer = new Timer(CheckConnectionTimeout, null, 
                TimeSpan.FromSeconds(ConnectionTimeoutSeconds), Timeout.InfiniteTimeSpan);

            Application.quitting += new Action(Disconnect);

            var connectPacket = new ConnectPacket
            {
                Type = (byte)PacketType.Connect,
                PlayerId = OwnPlayerId,
                Username = Main.Username
            };

            SendPacket(connectPacket);

            SrLogger.LogMessage("Connecting to the Server...",
                $"Connecting to {serverIp}:{port} as {OwnPlayerId}...");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error connecting to the Server: {ex}", SrLogTarget.Both);
            isConnected = false;
            throw;
        }
    }

    private void CheckConnectionTimeout(object? state)
    {
        if (!connectionAcknowledged && isConnected)
        {
            SrLogger.LogError("Connection timeout: Server did not respond within 10 seconds", SrLogTarget.Both);
            Disconnect();
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("UDP client is null in ReceiveLoop!", SrLogTarget.Both);
            return;
        }

        SrLogger.LogMessage("Client ReceiveLoop started!", SrLogTarget.Both);

        IPEndPoint remoteEp = udpClient.Client.AddressFamily switch
        {
            AddressFamily.InterNetwork     => new IPEndPoint(IPAddress.Any, 0),
            AddressFamily.InterNetworkV6   => new IPEndPoint(IPAddress.IPv6Any, 0),
            _ => throw new NotSupportedException("Unsupported address family")
        };

        while (isConnected)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEp);

                if (data.Length == 0)
                    continue;

                packetManager.HandlePacket(data);
                SrLogger.LogPacketSize($"Received {data.Length} bytes",
                    $"Received {data.Length} bytes from {remoteEp}");
            }
            catch (SocketException ex)
            {
                // This prevents WSAEINTR from logging, this is correct
                if (ex.ErrorCode != 10004)
                {
                    SrLogger.LogError($"ReceiveLoop error: Socket Exception:{ex.ErrorCode}\n{ex}");
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ReceiveLoop error: {ex}");
            }
        }

        SrLogger.LogMessage("Client ReceiveLoop ended!", SrLogTarget.Both);
    }

    public void SendChatMessage(string message)
    {
        if (!isConnected || string.IsNullOrEmpty(OwnPlayerId))
        {
            SrLogger.LogWarning("Cannot send chat message: Not connected to server!");
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            SrLogger.LogWarning("Cannot send empty chat message!");
            return;
        }

        var chatPacket = new ChatMessagePacket
        {
            Type = (byte)PacketType.ChatMessage,
            PlayerId = OwnPlayerId,
            Message = message
        };

        SendPacket(chatPacket);
    }

    internal static void StartHeartbeat()
    {
        // Removed this temporarily because there are no Handlers
        // heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(215), TimeSpan.FromSeconds(215));
    }

    private void SendHeartbeat(object? state)
    {
        if (!isConnected)
            return;
        
        var heartbeatPacket = new EmptyPacket 
        {
            Type = (byte)PacketType.Heartbeat 
        };
        
        SendPacket(heartbeatPacket);
    }

    internal void SendPacket<T>(T packet) where T : IPacket
    {
        if (udpClient == null || serverEndPoint == null || !isConnected)
        {
            SrLogger.LogWarning("Cannot send packet: Not connected to a Server!");
            return;
        }

        try
        {
            using var writer = new PacketWriter();
            packet.Serialise(writer);
            byte[] data = writer.ToArray();

            SrLogger.LogPacketSize($"Sending {data.Length} bytes to Server...", SrLogTarget.Both);

            var split = PacketChunkManager.SplitPacket(data);
            foreach (var chunk in split)
            {
                udpClient.Send(chunk, chunk.Length);
            }

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to Server in {split.Length} chunk(s).",
                SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet: {ex}", SrLogTarget.Both);
        }
    }

    public void Disconnect()
    {
        if (!isConnected)
            return;

        try
        {
            try
            {
                var leavePacket = new PlayerLeavePacket
                {
                    Type = (byte)PacketType.PlayerLeave,
                    PlayerId = OwnPlayerId
                };

                SendPacket(leavePacket);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Could not send leave packet: {ex.Message}");
            }
            
            isConnected = false;
            
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Dispose();
                heartbeatTimer = null;
            }

            if (connectionTimeoutTimer != null)
            {
                connectionTimeoutTimer.Dispose();
                connectionTimeoutTimer = null;
            }
            
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }

            // Give the receive thread a moment to exit normally
            if (receiveThread != null && receiveThread.IsAlive)
            {
                for (int i = 0; i < 20 && receiveThread.IsAlive; i++)
                {
                    System.Threading.Thread.Sleep(100);
                }

                if (receiveThread.IsAlive)
                {
                    SrLogger.LogWarning("Receive thread did not stop gracefully", SrLogTarget.Both);
                }
            }

            receiveThread = null;

            playerManager.Clear();

            SrLogger.LogMessage("Disconnected from server", SrLogTarget.Both);
            OnDisconnected?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during disconnect: {ex}", SrLogTarget.Both);
        }
    }

    internal void NotifyConnected()
    {
        connectionAcknowledged = true;
        
        if (connectionTimeoutTimer != null)
        {
            connectionTimeoutTimer.Dispose();
            connectionTimeoutTimer = null;
        }

        OnConnected?.Invoke(OwnPlayerId);
        isConnected = true;
    }

    internal void NotifyChatMessageReceived(string playerId, string message, DateTime timestamp)
    {
        OnChatMessageReceived?.Invoke(playerId, message, timestamp);
    }

    public static RemotePlayer? GetRemotePlayer(string playerId)
    {
        return playerManager.GetPlayer(playerId);
    }

    public static List<RemotePlayer> GetAllRemotePlayers()
    {
        return playerManager.GetAllPlayers();
    }
}