using System.Buffers;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using SR2MP.Api;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Components.UI;
using SR2MP.Packets.Api;
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
    // private Timer? heartbeatTimer;
    private ReliabilityManager? reliabilityManager;

    private volatile bool isConnected;
    private volatile bool connectionAcknowledged;
    private Timer? connectionTimeoutTimer;
    private const int ConnectionTimeoutSeconds = 10;

    private bool shownConnectionError;

    private readonly ClientPacketManager packetManager;

    public bool IsConnected => isConnected;
    public string PlayerId { get; private set; } = string.Empty;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<string, RemotePlayer>? OnPlayerUpdate;

    internal SR2MPClient()
    {
        packetManager = new ClientPacketManager(this);

        PlayerManager.OnPlayerAdded += playerId => OnPlayerJoined?.Invoke(playerId);
        PlayerManager.OnPlayerRemoved += playerId => OnPlayerLeft?.Invoke(playerId);
        PlayerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    internal void Connect(string serverIp, int port)
    {
        if (Main.Server.IsRunning)
        {
            SrLogger.LogWarning("You can not join a world while hosting a server.");
            return;
        }

        if (isConnected)
        {
            SrLogger.LogWarning("You are already connected to a Server!");
            return;
        }

        if (serverIp == "127.0.0.1" && !devMode)
        {
            SrLogger.LogWarning("You can not connect to this IP!");
            SrLogger.LogWarning("If you want to connect to someone on your local network, use their local IP!");
            SrLogger.LogWarning("To get the local IP, check your routers Website or use the 'ìpconfig' command prompt command!");
        }

        try
        {
            var parsedIp = IPAddress.Parse(serverIp);

            if (parsedIp.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (!Socket.OSSupportsIPv6)
                {
                    SrLogger.LogError("IPv6 is not supported on this machine! Please enable IPv6 or use an IPv4 address.");
                    throw new NotSupportedException("IPv6 is not available on this system");
                }

                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                SrLogger.LogMessage("Using IPv6 connection");
            }
            else
            {
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                SrLogger.LogMessage("Using IPv4 connection");
            }

            PacketDeduplication.Clear();

            serverEndPoint = new IPEndPoint(parsedIp, port);
            udpClient.Connect(serverEndPoint);

            udpClient.Client.ReceiveBufferSize = 512 * 1024;
            udpClient.Client.SendBufferSize = 512 * 1024;

            PlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();

            // Initialize reliability manager
            reliabilityManager = new ReliabilityManager(SendRaw);
            reliabilityManager.Start();

            packetManager.RegisterHandlers(Main.Core);

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
                PlayerId = PlayerId,
                Username = Main.Username,
                ModHashes = Mods.ToList().ConvertAll(mod => mod.Hash())
            };

            SendPacket(connectPacket);

            SrLogger.LogMessage("Connecting to the Server...",
                $"Connecting to {serverIp}:{port} as {PlayerId}...");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error connecting to the Server: {ex}");
            isConnected = false;
            throw;
        }
    }

    private void CheckConnectionTimeout(object? state)
    {
        if (connectionAcknowledged || !isConnected)
            return;

        SrLogger.LogError("Connection timeout: Server did not respond within 10 seconds");
        Disconnect();
    }

    internal void UpdateConnectionStatus(bool state)
    {
        isConnected = state;
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("UDP client is null in ReceiveLoop!");
            return;
        }

        SrLogger.LogMessage("Client ReceiveLoop started!");

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
                        SrLogger.LogError($"ReceiveLoop error: Socket Exception:{ex.ErrorCode}\n{ex}");
                    }

                    if (ex.ErrorCode == 10054 && !shownConnectionError)
                    {
                        SrLogger.LogError("The server is not running!\n" +
                                          "If the server is running, there is something wrong with PlayIt or your tunnel service.\n" +
                                          "If this is not the case, check your firewall settings");
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

        SrLogger.LogMessage("Client ReceiveLoop ended!");
    }

    // internal static void StartHeartbeat()
    // {
    //     // Removed this temporarily because there are no Handlers
    //     heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(215), TimeSpan.FromSeconds(215));
    // }

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
        => PrepareAndSend(packet, packet.Reliability, (byte)packet.Type, SerialiseInternalPacket<T>.Serialiser);

    /// <summary>
    /// Sends a packet over the network.
    /// </summary>
    /// <typeparam name="T">The type of the packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    [PublicAPI]
    public void SendData<T>(T data) where T : ICustomPacket
    {
        if (!ApiHandlers.PacketTypeMap.TryGetValue(data.GetType(), out var modId))
        {
            SrLogger.LogWarning($"Cannot send API packet: No ModId registered for custom packet type {data.GetType().FullName}.");
            return;
        }

        var apiHeader = new ApiPacket(data.Reliability, modId);
        PrepareAndSend((apiHeader, data), apiHeader.Reliability, (byte)apiHeader.Type, SerialiseApiPacket<T>.Serialiser);
    }

    private void PrepareAndSend<T>(T state, PacketReliability reliability, byte packetType, PacketWriterDelegate<T> writeAction)
    {
        if (udpClient == null || serverEndPoint == null || !isConnected)
        {
            SrLogger.LogWarning("Cannot send packet: Not connected to a Server!");
            return;
        }

        var writer = PacketWriter.Borrow();

        try
        {
            writeAction(writer, state);
            var data = writer.ToSpan();
            SrLogger.LogPacketSize($"Sending {data.Length} bytes to Server...");

            ushort sequenceNumber = 0;

            // Get sequence number for ordered packets (pass packet type)
            if (reliability == PacketReliability.ReliableOrdered)
                sequenceNumber = reliabilityManager?.GetNextSequenceNumber(packetType) ?? 0;

            var splitResult = PacketChunkManager.SplitPacket(data, reliability, sequenceNumber, out var packetId);

            if (reliability != PacketReliability.Unreliable)
                reliabilityManager?.TrackPacket(splitResult, serverEndPoint, packetId, data[0], reliability);

            for (var i = 0; i < splitResult.Count; i++)
                SendRaw(splitResult.Chunks[i], serverEndPoint);

            if (reliability == PacketReliability.Unreliable)
                splitResult.Dispose();

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to Server in {splitResult.Count} chunk(s) (ID={packetId}).");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet: {ex}");
        }
        finally
        {
            PacketWriter.Return(writer);
        }
    }

    // Sends raw data without reliability tracking (used for resends)
    private void SendRaw(ArraySegment<byte> data, IPEndPoint endPoint)
    {
        if (data.Array != null)
            udpClient?.Client.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, endPoint);
    }

    // Handle acknowledgement from server, used in client packet manager
    internal void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        reliabilityManager?.HandleAck(sender, packetId, packetType);
    }

    // Check if ordered packet should be processed
    internal bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType)
    {
        return reliabilityManager?.ShouldProcessOrderedPacket(sender, sequenceNumber, packetType) ?? true;
    }

    internal void Disconnect()
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
                    PlayerId = PlayerId
                };

                SendPacket(leavePacket);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Could not send leave packet: {ex.Message}");
            }

            PacketDeduplication.Clear();

            isConnected = false;

            // heartbeatTimer?.Dispose();
            // heartbeatTimer = null;

            connectionTimeoutTimer?.Dispose();
            connectionTimeoutTimer = null;

            reliabilityManager?.Stop();

            udpClient?.Close();
            udpClient = null;

            if (receiveThread is { IsAlive: true })
            {
                SrLogger.LogWarning("Receive thread did not stop gracefully");
            }

            receiveThread = null;

            foreach (var player in PlayerManager.GetAllPlayers())
            {
                var playerId = player.PlayerId;

                if (!PlayerObjects.TryGetValue(playerId, out var playerObject))
                    continue;

                if (playerObject)
                {
                    Object.Destroy(playerObject);
                    SrLogger.LogPacketSize($"Destroyed player object for {playerId}");
                }

                PlayerObjects.Remove(playerId);
            }

            PlayerManager.Clear();
            NetworkStringPool.Clear();

            SrLogger.LogMessage("Disconnected from server");
            OnDisconnected?.Invoke();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during disconnect: {ex}");
        }
    }

    internal void NotifyConnected()
    {
        connectionAcknowledged = true;

        connectionTimeoutTimer?.Dispose();
        connectionTimeoutTimer = null;

        OnConnected?.Invoke(PlayerId);
    }

    // public static RemotePlayer? GetRemotePlayer(string playerId)
    // {
    //     return playerManager.GetPlayer(playerId);
    // }

    // public static List<RemotePlayer> GetAllRemotePlayers()
    // {
    //     return playerManager.GetAllPlayers();
    // }

    // public int GetPendingReliablePackets() => reliabilityManager?.GetPendingPacketCount() ?? 0;
}