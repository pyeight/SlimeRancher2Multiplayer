using LiteNetLib;
using LiteNetLib.Utils;
using SR2MP.Components.UI;
using SR2MP.Networking;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using SR2MP.Shared.Utils;

namespace SR2MP.Server;

public sealed class Server
{
    private NetManager? netManager;
    private EventBasedNetListener? listener;
    private readonly ClientManager clientManager;
    private readonly PacketManager packetManager;

    private Timer? timeoutTimer;

    // Just here so that the port is viewable.
    public int Port { get; private set; }

    public event Action? OnServerStarted;

    public Server()
    {
        clientManager = new ClientManager();
        packetManager = new PacketManager(clientManager);

        clientManager.OnClientRemoved += OnClientRemoved;
    }

    public int GetClientCount() => clientManager.ClientCount;

    public bool IsRunning() => netManager?.IsRunning ?? false;

    public void Start(int port, bool enableIPv6)
    {
        if (Main.Client.IsConnected)
        {
            SrLogger.LogWarning("You are already connected to a server, restart your game to host your own server");
            return;
        }

        if (IsRunning())
        {
            SrLogger.LogMessage("Server is already running!", SrLogTarget.Both);
            return;
        }

        try
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener)
            {
                IPv6Enabled = enableIPv6,
                ChannelsCount = NetChannels.Count,
                UnconnectedMessagesEnabled = true,
                PingInterval = 1000,
                DisconnectTimeout = 10000
            };

            packetManager.RegisterHandlers();

            listener.ConnectionRequestEvent += request =>
            {
                var acceptedPeer = request.AcceptIfKey(ProtocolConstants.ConnectionKey);
                if (acceptedPeer == null)
                {
                    SrLogger.LogWarning($"Rejected connection with wrong protocol key from {request.RemoteEndPoint}", SrLogTarget.Both);
                    request.Reject();
                }
            };

            listener.PeerConnectedEvent += peer =>
            {
                SrLogger.LogMessage($"Peer connected: {peer.Address}:{peer.Port}", SrLogTarget.Both);
            };

            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                SrLogger.LogWarning($"Peer disconnected: {peer.Address}:{peer.Port} ({info.Reason})", SrLogTarget.Both);
                clientManager.RemoveClient(peer);
            };

            listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                packetManager.Handle(reader, peer);
            };

            listener.NetworkReceiveUnconnectedEvent += (remoteEndPoint, reader, messageType) =>
            {
                if (!ServerInfoProtocol.TryReadRequest(reader))
                    return;

                var players = playerManager
                    .GetAllPlayers()
                    .Select(player => (player.PlayerId, player.Username ?? string.Empty))
                    .ToList();

                var writer = new NetDataWriter();
                ServerInfoProtocol.WriteResponse(writer, players);
                netManager?.SendUnconnectedMessage(writer, remoteEndPoint);
            };

            listener.NetworkErrorEvent += (endPoint, socketError) =>
            {
                SrLogger.LogError($"Network error {socketError} from {endPoint}", SrLogTarget.Both);
            };

            netManager.Start(port);
            Port = port;

            Application.quitting += new Action(Close);
            // timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            OnServerStarted?.Invoke();

            int randomComponent = UnityEngine.Random.Range(0, 999999999);
            MultiplayerUI.Instance.RegisterSystemMessage(
                "The world is now open to others!",
                $"SYSTEM_HOST_START_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{randomComponent}",
                MultiplayerUI.SystemMessageConnect
            );
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start server: {ex}", SrLogTarget.Both);
        }
    }

    public void PollEvents()
    {
        netManager?.PollEvents();
    }

    private void OnClientRemoved(ClientInfo client)
    {
        if (string.IsNullOrEmpty(client.PlayerId))
            return;

        var leavePacket = new PlayerLeavePacket
        {
            Kind = PacketType.BroadcastPlayerLeave,
            PlayerId = client.PlayerId
        };

        SendToAll(leavePacket);

        SrLogger.LogMessage($"Player left broadcast sent for: {client.PlayerId}", SrLogTarget.Both);
    }

    // private void CheckTimeouts(object? state)
    // {
    //     try
    //     {
    //         clientManager.RemoveTimedOutClients();
    //     }
    //     catch (Exception ex)
    //     {
    //         SrLogger.LogError($"Error checking timeouts: {ex}");
    //     }
    // }

    public void Close()
    {
        if (!IsRunning())
            return;

        var closeChatMessage = new ChatMessagePacket
        {
            Username = "SYSTEM",
            Message = "Server closed!",
            MessageID = $"SYSTEM_CLOSE_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            MessageType = MultiplayerUI.SystemMessageClose
        };
        SendToAll(closeChatMessage);

        MultiplayerUI.Instance.ClearChatMessages();
        int randomComponent = UnityEngine.Random.Range(0, 999999999);
        MultiplayerUI.Instance.RegisterSystemMessage("You closed the server!", $"SYSTEM_CLOSE_HOST_{randomComponent}", MultiplayerUI.SystemMessageClose);

        try
        {
            timeoutTimer?.Dispose();
            timeoutTimer = null;

            var closePacket = new ClosePacket();
            SendToAll(closePacket);

            var allPlayerIds = playerManager.GetAllPlayers().Select(p => p.PlayerId).ToList();
            foreach (var playerId in allPlayerIds)
            {
                if (playerObjects.TryGetValue(playerId, out var playerObject))
                {
                    if (playerObject != null)
                    {
                        Object.Destroy(playerObject);
                        SrLogger.LogPacketSize($"Destroyed player object for {playerId}", SrLogTarget.Both);
                    }
                    playerObjects.Remove(playerId);
                }
            }

            clientManager.Clear();
            playerManager.Clear();

            netManager?.Stop();
            netManager = null;
            listener = null;

            SrLogger.LogMessage("Server closed", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during server shutdown: {ex}", SrLogTarget.Both);
        }
    }

    public void SendToClient<T>(T packet, NetPeer peer) where T : PacketBase
    {
        if (netManager == null)
            return;

        var writer = new NetDataWriter();
        packetManager.Processor.WriteNetSerializable(writer, ref packet);
        var delivery = NetDeliveryRegistry.Get<T>();
        peer.Send(writer, delivery.Channel, delivery.Method);
    }

    public void SendToClient<T>(T packet, ClientInfo client) where T : PacketBase
    {
        SendToClient(packet, client.Peer);
    }

    public void SendToAll<T>(T packet) where T : PacketBase
    {
        if (netManager == null)
            return;

        var writer = new NetDataWriter();
        packetManager.Processor.WriteNetSerializable(writer, ref packet);
        var delivery = NetDeliveryRegistry.Get<T>();

        foreach (var client in clientManager.GetAllClients())
        {
            client.Peer.Send(writer, delivery.Channel, delivery.Method);
        }
    }

    public void SendToAllExcept<T>(T packet, NetPeer excludePeer) where T : PacketBase
    {
        if (netManager == null)
            return;

        var writer = new NetDataWriter();
        packetManager.Processor.WriteNetSerializable(writer, ref packet);
        var delivery = NetDeliveryRegistry.Get<T>();

        foreach (var client in clientManager.GetAllClients())
        {
            if (client.Peer.Id == excludePeer.Id)
                continue;

            client.Peer.Send(writer, delivery.Channel, delivery.Method);
        }
    }
}
