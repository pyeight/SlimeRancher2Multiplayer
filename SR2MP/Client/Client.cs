using System.Buffers;
using System.Net;
using System.Net.Sockets;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Components.UI;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Client;

public sealed class SR2MPClient
{
    private UdpClient? udpClient;
    private IPEndPoint? serverEndPoint;
    private Thread? receiveThread;
    private Timer? heartbeatTimer;
    private ReliabilityManager? reliabilityManager;

    private volatile bool isConnected;
    private volatile bool connectionAcknowledged;
    private Timer? connectionTimeoutTimer;
    private const int ConnectionTimeoutSeconds = 10;

    private bool shownConnectionError;

    private readonly ClientPacketManager packetManager;

    public bool IsConnected => isConnected;
    public string OwnPlayerId { get; private set; } = string.Empty;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<string, RemotePlayer>? OnPlayerUpdate;

    public SR2MPClient()
    {
        packetManager = new ClientPacketManager(this);

        playerManager.OnPlayerAdded += playerId => OnPlayerJoined?.Invoke(playerId);
        playerManager.OnPlayerRemoved += playerId => OnPlayerLeft?.Invoke(playerId);
        playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    public void Connect(string serverIp, int port)
    {
        if (Main.Server.IsRunning())
        {
            SrLogger.LogWarning("You can not join a world while hosting a server.", SrLogTarget.Both);
            return;
        }

        if (isConnected)
        {
            SrLogger.LogWarning("You are already connected to a Server!", SrLogTarget.Both);
            return;
        }

        try
        {
            var parsedIp = IPAddress.Parse(serverIp);

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

            PacketDeduplication.Clear();

            serverEndPoint = new IPEndPoint(parsedIp, port);
            udpClient.Connect(serverEndPoint);

            udpClient.Client.ReceiveBufferSize = 512 * 1024;
            udpClient.Client.SendBufferSize = 512 * 1024;

            OwnPlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();

            // Initialize reliability manager
            reliabilityManager = new ReliabilityManager(SendRaw);
            reliabilityManager.Start();

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
        if (connectionAcknowledged || !isConnected)
            return;
        SrLogger.LogError("Connection timeout: Server did not respond within 10 seconds", SrLogTarget.Both);
        Disconnect();
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("UDP client is null in ReceiveLoop!", SrLogTarget.Both);
            return;
        }

        SrLogger.LogMessage("Client ReceiveLoop started!", SrLogTarget.Both);

        EndPoint remoteEp = udpClient.Client.AddressFamily switch
        {
            AddressFamily.InterNetwork => new IPEndPoint(IPAddress.Any, 0),
            AddressFamily.InterNetworkV6 => new IPEndPoint(IPAddress.IPv6Any, 0),
            _ => throw new NotSupportedException("Unsupported address family")
        };

        var receiveBuffer = ArrayPool<byte>.Shared.Rent(2048);

        try
        {
            while (isConnected)
            {
                try
                {
                    var receivedBytes = udpClient.Client.ReceiveFrom(receiveBuffer, ref remoteEp);

                    if (receivedBytes == 0)
                        continue;

                    packetManager.HandlePacket(receiveBuffer, receivedBytes, (IPEndPoint)remoteEp);
                    SrLogger.LogPacketSize($"Received {receivedBytes} bytes",
                        $"Received {receivedBytes} bytes from {remoteEp}");
                }
                catch (SocketException ex)
                {
                    // This prevents WSAEINTR from logging, this is correct
                    if (ex.ErrorCode is not 10004 and not 10054)
                    {
                        SrLogger.LogError($"ReceiveLoop error: Socket Exception:{ex.ErrorCode}\n{ex}", SrLogTarget.Both);
                    }

                    if (ex.ErrorCode == 10054 && !shownConnectionError)
                    {
                        SrLogger.LogError("The server is not running!\n" +
                                          "If the server is running, there is something wrong with PlayIt or your tunnel service.\n" +
                                          "If this is not the case, check your firewall settings", SrLogTarget.Both);
                        shownConnectionError = true;
                    }

                    MultiplayerUI.Instance.RegisterSystemMessage("Could not join the world, check the MelonLoader console for details", $"SYSTEM_JOIN_10054_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageClose);
                    Disconnect();
                }
                catch (Exception ex)
                {
                    SrLogger.LogError($"ReceiveLoop error: {ex}");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }

        SrLogger.LogMessage("Client ReceiveLoop ended!", SrLogTarget.Both);
        }

    internal static void StartHeartbeat()
    {
        // Removed this temporarily because there are no Handlers
        // heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(215), TimeSpan.FromSeconds(215));
    }

    // private void SendHeartbeat(object? state)
    // {
    //     if (!isConnected)
    //         return;

    //     var heartbeatPacket = new EmptyPacket
    //     {
    //         Type = PacketType.Heartbeat
    //     };

    //     SendPacket(heartbeatPacket);
    // }

    internal void SendPacket<T>(T packet) where T : IPacket
    {
        if (udpClient == null || serverEndPoint == null || !isConnected)
        {
            SrLogger.LogWarning("Cannot send packet: Not connected to a Server!");
            return;
        }

        var writer = PacketBufferPool.GetWriter();

        try
        {
            writer.WritePacket(packet);
            var data = writer.ToSpan();

            SrLogger.LogPacketSize($"Sending {data.Length} bytes to Server...", SrLogTarget.Both);

            var reliability = packet.Reliability;
            ushort sequenceNumber = 0;

            // Get sequence number for ordered packets (pass packet type)
            if (reliability == PacketReliability.ReliableOrdered)
                sequenceNumber = reliabilityManager?.GetNextSequenceNumber((byte)packet.Type) ?? 0;

            var splitResult = PacketChunkManager.SplitPacket(data, reliability, sequenceNumber, out var packetId);
            
            if (reliability != PacketReliability.Unreliable)
                reliabilityManager?.TrackPacket(splitResult, serverEndPoint, packetId, data[0], reliability, sequenceNumber);

            for (var i = 0; i < splitResult.Count; i++)
                SendRaw(splitResult.Chunks[i], serverEndPoint);

            if (reliability == PacketReliability.Unreliable)
                splitResult.Dispose();

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to Server in {splitResult.Count} chunk(s) (ID={packetId}).",
                SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet: {ex}", SrLogTarget.Both);
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }

    // Sends raw data without reliability tracking (used for resends)
    private void SendRaw(ArraySegment<byte> data, IPEndPoint _)
    {
        if (data.Array == null) return;
        udpClient?.Client.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
    }

    // Handle acknowledgement from server, used in client packet manager
    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        reliabilityManager?.HandleAck(sender, packetId, packetType);
    }

    // Check if ordered packet should be processed
    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType)
    {
        return reliabilityManager?.ShouldProcessOrderedPacket(sender, sequenceNumber, packetType) ?? true;
    }

    public void Disconnect()
    {
        if (!isConnected)
            return;

        try
        {
            MultiplayerUI.Instance.ClearChatMessages();
            if (!shownConnectionError)
            {
                MultiplayerUI.Instance.RegisterSystemMessage("You disconnected from the world!", $"SYSTEM_DISCONNECT_LOCAL_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageDisconnect);
            }
            try
            {
                var leavePacket = new PlayerLeavePacket
                {
                    Type = PacketType.PlayerLeave,
                    PlayerId = OwnPlayerId
                };

                SendPacket(leavePacket);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Could not send leave packet: {ex.Message}");
            }

            PacketDeduplication.Clear();

            isConnected = false;

            heartbeatTimer?.Dispose();
            heartbeatTimer = null;

            connectionTimeoutTimer?.Dispose();
            connectionTimeoutTimer = null;

            reliabilityManager?.Stop();

            udpClient?.Close();
            udpClient = null;

            if (receiveThread is { IsAlive: true })
            {
                SrLogger.LogWarning("Receive thread did not stop gracefully", SrLogTarget.Both);
            }

            receiveThread = null;

            foreach (var player in playerManager.GetAllPlayers())
            {
                var playerId = player.PlayerId;
                if (!playerObjects.TryGetValue(playerId, out var playerObject))
                    continue;
                
                if (playerObject)
                {
                    Object.Destroy(playerObject);
                    SrLogger.LogPacketSize($"Destroyed player object for {playerId}", SrLogTarget.Both);
                }
                playerObjects.Remove(playerId);
            }

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

        connectionTimeoutTimer?.Dispose();
        connectionTimeoutTimer = null;

        OnConnected?.Invoke(OwnPlayerId);
    }

    public static RemotePlayer? GetRemotePlayer(string playerId)
    {
        return playerManager.GetPlayer(playerId);
    }

    public static List<RemotePlayer> GetAllRemotePlayers()
    {
        return playerManager.GetAllPlayers();
    }

    public int GetPendingReliablePackets() => reliabilityManager?.GetPendingPacketCount() ?? 0;
}