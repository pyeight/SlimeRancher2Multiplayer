using System.Net;
using System.Net.Sockets;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Packets.C2S;
using SR2MP.Packets.S2C;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Client;

public sealed class Client
{
    private UdpClient? udpClient;
    private IPEndPoint? serverEndPoint;
    private Thread? receiveThread;
    private Timer? heartbeatTimer;
    private volatile bool isConnected;

    private readonly ClientPacketManager packetManager;
    private readonly RemotePlayerManager playerManager;

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
        playerManager = new RemotePlayerManager();
        packetManager = new ClientPacketManager(this, playerManager);

        playerManager.OnPlayerAdded += (playerId) => OnPlayerJoined?.Invoke(playerId);
        playerManager.OnPlayerRemoved += (playerId) => OnPlayerLeft?.Invoke(playerId);
        playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    public void Connect(string serverIp, int port)
    {
        if (isConnected)
        {
            SrLogger.LogMessage("You are already connected to a Server!", SrLogger.LogTarget.Both);
            return;
        }

        try
        {
            IPAddress parsedIp = IPAddress.Parse(serverIp);

            if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (!Socket.OSSupportsIPv6)
                {
                    SrLogger.LogError("IPv6 is not supported on this machine! Please enable IPv6 or use an IPv4 address.", SrLogger.LogTarget.Both);
                    throw new NotSupportedException("IPv6 is not available on this system");
                }
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                SrLogger.LogMessage("Using IPv6 connection", SrLogger.LogTarget.Both);
            }
            else
            {
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                SrLogger.LogMessage("Using IPv4 connection", SrLogger.LogTarget.Both);
            }

            serverEndPoint = new IPEndPoint(parsedIp, port);
            udpClient.Connect(serverEndPoint);

            OwnPlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();

            packetManager.RegisterHandlers();

            isConnected = true;

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            var connectPacket = new ConnectPacket
            {
                Type = (byte)PacketType.Connect,
                PlayerId = OwnPlayerId
            };

            SendPacket(connectPacket);

            SrLogger.LogMessage("Connecting to the Server...",
                $"Connecting to {serverIp}:{port} as {OwnPlayerId}...");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error connecting to the Server: {ex}", SrLogger.LogTarget.Both);
            isConnected = false;
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("UDP client is null in ReceiveLoop!", SrLogger.LogTarget.Both);
            return;
        }
        SrLogger.LogMessage("Client ReceiveLoop started!", SrLogger.LogTarget.Both);

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);

        while (isConnected)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    packetManager.HandlePacket(data);
                    SrLogger.LogMessage($"Received {data.Length} bytes",
                        $"Received {data.Length} bytes from {remoteEP}");
                }
            }
            catch (SocketException)
            {
                if (!isConnected)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ReceiveLoop error: {ex}");
            }
        }
    }

    public void SendPlayerUpdate(
        UnityEngine.Vector3 position,
        UnityEngine.Quaternion rotation,
        float horizontalMovement = 0f,
        float forwardMovement = 0f,
        float yaw = 0f,
        int airborneState = 0,
        bool moving = false,
        float horizontalSpeed = 0f,
        float forwardSpeed = 0f,
        bool sprinting = false)
    {
        if (!isConnected || string.IsNullOrEmpty(OwnPlayerId))
            return;

        var updatePacket = new PlayerUpdatePacket
        {
            Type = (byte)PacketType.PlayerUpdate,
            PlayerId = OwnPlayerId,
            Position = position,
            Rotation = rotation,
            HorizontalMovement = horizontalMovement,
            ForwardMovement = forwardMovement,
            Yaw = yaw,
            AirborneState = airborneState,
            Moving = moving,
            HorizontalSpeed = horizontalSpeed,
            ForwardSpeed = forwardSpeed,
            Sprinting = sprinting
        };

        SendPacket(updatePacket);
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

    internal void StartHeartbeat()
    {
        heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private void SendHeartbeat(object? state)
    {
        if (!isConnected)
            return;

        var heartbeatPacket = new HeartbeatPacket
        {
            Type = (byte)PacketType.Heartbeat
        };

        SendPacket(heartbeatPacket);
    }

    internal void SendPacket(IPacket packet)
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

            udpClient.Send(data, data.Length);
            SrLogger.LogMessage($"Sent {data.Length} bytes to Server.", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet: {ex}", SrLogger.LogTarget.Both);
        }
    }

    public void Disconnect()
    {
        if (!isConnected)
            return;

        try
        {
            var leavePacket = new PlayerLeavePacket
            {
                Type = (byte)PacketType.PlayerLeave,
                PlayerId = OwnPlayerId
            };

            SendPacket(leavePacket);

            heartbeatTimer?.Dispose();
            heartbeatTimer = null;

            isConnected = false;
            udpClient?.Close();

            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SrLogger.LogWarning("Receive thread did not stop gracefully", SrLogger.LogTarget.Both);
                }
            }

            playerManager.Clear();

            SrLogger.LogMessage("Disconnected from server", SrLogger.LogTarget.Both);
            OnDisconnected?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during disconnect: {ex}", SrLogger.LogTarget.Both);
        }
    }

    internal void NotifyConnected()
    {
        OnConnected?.Invoke(OwnPlayerId);
    }

    internal void NotifyChatMessageReceived(string playerId, string message, DateTime timestamp)
    {
        OnChatMessageReceived?.Invoke(playerId, message, timestamp);
    }

    public RemotePlayer? GetRemotePlayer(string playerId)
    {
        return playerManager.GetPlayer(playerId);
    }

    public List<RemotePlayer> GetAllRemotePlayers()
    {
        return playerManager.GetAllPlayers();
    }
}