using System.Net;
using System.Net.Sockets;
using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Client.Models;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Client;

public sealed class Client
{
    private UdpClient? udpClient;
    private IPEndPoint? serverEndPoint;
    private Il2CppSystem.Threading.Thread? receiveThread;
    private Timer? heartbeatTimer;
    private volatile bool isConnected;

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
        packetManager = new ClientPacketManager(this, GlobalVariables.playerManager);

        GlobalVariables.playerManager.OnPlayerAdded += (playerId) => OnPlayerJoined?.Invoke(playerId);
        GlobalVariables.playerManager.OnPlayerRemoved += (playerId) => OnPlayerLeft?.Invoke(playerId);
        GlobalVariables.playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
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
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
            else
                udpClient = new UdpClient(AddressFamily.InterNetwork);

            serverEndPoint = new IPEndPoint(parsedIp, port);
            udpClient.Connect(serverEndPoint);

            OwnPlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();
            packetManager.RegisterHandlers();

            isConnected = true;
            StartReceiveThread();

            Application.quitting += new Action(Disconnect);

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

    private void StartReceiveThread()
    {
        receiveThread = new Il2CppSystem.Threading.Thread(new Action(ReceiveLoop));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (isConnected && udpClient != null)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                if (data.Length > 0)
                {
                    OnDataReceived(data);
                }
            }
            catch (SocketException)
            {
                if (!isConnected) return;
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Client ReceiveLoop error: {ex}");
            }
        }
    }

    private void OnDataReceived(byte[] data)
    {
        packetManager.HandlePacket(data);
        SrLogger.LogPacketSize($"Received {data.Length} bytes",
            $"Received {data.Length} bytes from Server");
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
        if (udpClient == null || !isConnected)
        {
            SrLogger.LogWarning("Cannot send packet: Not connected to a Server!");
            return;
        }

        try
        {
            using var writer = new PacketWriter();
            packet.Serialise(writer);
            byte[] data = writer.ToArray();

            SrLogger.LogPacketSize($"Sending {data.Length} bytes to Server...", SrLogger.LogTarget.Both);

            udpClient.Send(data, data.Length);

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to Server.",
                SrLogger.LogTarget.Both);
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
            udpClient = null;

            GlobalVariables.playerManager.Clear();

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
        return GlobalVariables.playerManager.GetPlayer(playerId);
    }

    public List<RemotePlayer> GetAllRemotePlayers()
    {
        return GlobalVariables.playerManager.GetAllPlayers();
    }

    public class PendingJoinData
    {
        public string PlayerId;
        public int Money;
        public int RainbowMoney;
        public List<string> OtherPlayers;
    }

    public PendingJoinData? PendingJoin { get; set; }
}
