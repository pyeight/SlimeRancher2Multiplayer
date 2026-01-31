using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using SR2MP.Client.Managers;
using SR2MP.Client.Models;
using SR2MP.Components.UI;
using SR2MP.Networking;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Client;

public sealed class Client
{
    private EventBasedNetListener? listener;
    private NetManager? netManager;
    private NetPeer? serverPeer;

    private volatile bool isConnected;
    private volatile bool connectionAcknowledged;
    private Timer? connectionTimeoutTimer;
    private const int ConnectionTimeoutSeconds = 10;

    private readonly ClientPacketManager packetManager;
    private Action<List<(string PlayerId, string Username)>>? pendingServerInfoCallback;

    public string OwnPlayerId { get; private set; } = string.Empty;
    public bool IsConnected => isConnected;

    public event Action<string>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnPlayerJoined;
    public event Action<string>? OnPlayerLeft;
    public event Action<string, RemotePlayer>? OnPlayerUpdate;

    public Client()
    {
        packetManager = new ClientPacketManager(this, playerManager);

        playerManager.OnPlayerAdded += playerId => OnPlayerJoined?.Invoke(playerId);
        playerManager.OnPlayerRemoved += playerId => OnPlayerLeft?.Invoke(playerId);
        playerManager.OnPlayerUpdated += (playerId, player) => OnPlayerUpdate?.Invoke(playerId, player);
    }

    public void Connect(string serverIp, int port)
    {
        if (Main.Server.IsRunning())
        {
            SrLogger.LogWarning("You are already hosting a server, to connect to someone else, restart your game.");
            return;
        }

        if (isConnected)
        {
            SrLogger.LogMessage("You are already connected to a Server!", SrLogTarget.Both);
            return;
        }

        try
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener)
            {
                ChannelsCount = NetChannels.Count,
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true,
                PingInterval = 1000,
                DisconnectTimeout = 10000
            };

            packetManager.RegisterHandlers();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += (peer, reader, channel, method) => packetManager.Handle(reader);
            listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
            listener.NetworkErrorEvent += (endPoint, socketError) => SrLogger.LogError($"Network error {socketError} from {endPoint}", SrLogTarget.Both);

            netManager.Start();
            serverPeer = netManager.Connect(serverIp, port, ProtocolConstants.ConnectionKey);

            isConnected = true;
            connectionAcknowledged = false;

            connectionTimeoutTimer = new Timer(CheckConnectionTimeout, null,
                TimeSpan.FromSeconds(ConnectionTimeoutSeconds), Timeout.InfiniteTimeSpan);

            Application.quitting += new Action(Disconnect);

            SrLogger.LogMessage("Connecting to the Server...",
                $"Connecting to {serverIp}:{port}...");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error connecting to the Server: {ex}", SrLogTarget.Both);
            isConnected = false;
            throw;
        }
    }

    private void OnPeerConnected(NetPeer peer)
    {
        serverPeer = peer;
        OwnPlayerId = PlayerIdGenerator.GeneratePersistentPlayerId();

        var connectPacket = new ConnectPacket
        {
            PlayerId = OwnPlayerId,
            Username = Main.Username
        };

        SendPacket(connectPacket);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
    {
        SrLogger.LogWarning($"Disconnected from server: {info.Reason}", SrLogTarget.Both);
        Disconnect();
    }

    private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (pendingServerInfoCallback == null)
            return;

        if (ServerInfoProtocol.TryReadResponse(reader, out var players))
        {
            var callback = pendingServerInfoCallback;
            pendingServerInfoCallback = null;
            callback?.Invoke(players);
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

    internal void SendPacket<T>(T packet) where T : PacketBase
    {
        if (serverPeer == null || netManager == null || !isConnected)
        {
            SrLogger.LogWarning("Cannot send packet: Not connected to a Server!");
            return;
        }

        try
        {
            var writer = new NetDataWriter();
            packetManager.Processor.WriteNetSerializable(writer, ref packet);
            var delivery = NetDeliveryRegistry.Get<T>();
            serverPeer.Send(writer, delivery.Channel, delivery.Method);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet: {ex}", SrLogTarget.Both);
        }
    }

    public void PollEvents()
    {
        netManager?.PollEvents();
    }

    public void Disconnect()
    {
        if (!isConnected)
            return;

        try
        {
            MultiplayerUI.Instance.ClearChatMessages();
            int randomComponent = UnityEngine.Random.Range(0, 999999999);
            MultiplayerUI.Instance.RegisterSystemMessage("You disconnected from the world!", $"SYSTEM_DISCONNECT_LOCAL_{randomComponent}", MultiplayerUI.SystemMessageDisconnect);
            try
            {
                var leavePacket = new PlayerLeavePacket
                {
                    Kind = PacketType.PlayerLeave,
                    PlayerId = OwnPlayerId
                };

                SendPacket(leavePacket);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Could not send leave packet: {ex.Message}");
            }

            isConnected = false;

            if (connectionTimeoutTimer != null)
            {
                connectionTimeoutTimer.Dispose();
                connectionTimeoutTimer = null;
            }

            netManager?.Stop();
            netManager = null;
            listener = null;
            serverPeer = null;

            var allPlayerIds = playerManager.GetAllPlayers().Select(p => p.PlayerId).ToList();
            foreach (var playerId in allPlayerIds)
            {
                if (playerObjects.TryGetValue(playerId, out var playerObject))
                {
                    if (playerObject)
                    {
                        Object.Destroy(playerObject);
                        SrLogger.LogPacketSize($"Destroyed player object for {playerId}", SrLogTarget.Both);
                    }
                    playerObjects.Remove(playerId);
                }
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

        if (connectionTimeoutTimer != null)
        {
            connectionTimeoutTimer.Dispose();
            connectionTimeoutTimer = null;
        }

        OnConnected?.Invoke(OwnPlayerId);
    }

    public static RemotePlayer? GetRemotePlayer(string playerId)
    {
        return playerManager.GetPlayer(playerId);
    }

    public void QueryServerInfo(string serverIp, int port, Action<List<(string PlayerId, string Username)>> onSuccess)
    {
        if (netManager == null)
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener)
            {
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true
            };

            listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
            netManager.Start();
        }

        pendingServerInfoCallback = onSuccess;
        var writer = new NetDataWriter();
        ServerInfoProtocol.WriteRequest(writer);
        netManager.SendUnconnectedMessage(writer, serverIp, port);
    }

    public static List<RemotePlayer> GetAllRemotePlayers()
    {
        return playerManager.GetAllPlayers().ToList();
    }
}
