using System.Net;
using JetBrains.Annotations;
using SR2MP.Api;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Api;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server;

[PublicApi]
public sealed class SR2MPServer
{
    internal readonly NetworkManager NetworkManager;
    internal readonly ClientManager ClientManager;
    internal readonly ReSyncManager ReSyncManager;

    private readonly ServerPacketManager packetManager;

    private Timer? timeoutTimer;

    // Just here so that the port is viewable.

    /// <summary>
    /// Gets or sets a value that denotes the server's port.
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// Gets or sets a value that denotes the host player's player ID.
    /// </summary>
    public string PlayerId { get; private set; } = string.Empty;

    /// <summary>
    /// An event that is invoked when the server connects.
    /// </summary>
    public event Action? OnServerStarted;

    /// <summary>
    /// Gets the number of clients currently connected.
    /// </summary>
    public int GetClientCount => ClientManager.ClientCount;

    /// <summary>
    /// Gets a value indicating the running status of the server.
    /// </summary>
    public bool IsRunning => NetworkManager.IsRunning;

    internal SR2MPServer()
    {
        NetworkManager = new NetworkManager();
        ClientManager = new ClientManager();
        ReSyncManager = new ReSyncManager();
        packetManager = new ServerPacketManager(NetworkManager, ClientManager);

        NetworkManager.OnDataReceived += OnDataReceived;
        ClientManager.OnClientRemoved += OnClientRemoved;
    }

    internal void Start(int port, bool enableIPv6)
    {
        if (Main.Client.IsConnected)
        {
            SrLogger.LogWarning("You are already connected to a server, restart your game to host your own server");
            return;
        }

        if (NetworkManager.IsRunning)
        {
            SrLogger.LogMessage("Server is already running!");
            return;
        }

        try
        {
            PlayerId = DevMode ? "PLAYER_TEST_MODE" : PlayerIdGenerator.GeneratePersistentPlayerId();

            packetManager.RegisterHandlers(Main.Core);
            Application.quitting += new Action(Close);
            NetworkManager.Start(port, enableIPv6);
            this.Port = port;
            // Commented because we don't need this yet
            // timeoutTimer = new Timer(CheckTimeouts, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            OnServerStarted?.Invoke();
            MultiplayerUI.Instance.RegisterSystemMessage(
                "The world is now open to others!",
                $"SYSTEM_HOST_START_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                MultiplayerUI.SystemMessageConnect
            );
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start server: {ex}");
        }
    }

    private void OnDataReceived(byte[] data, int receivedBytes, IPEndPoint clientEp)
    {
        SrLogger.LogPacketSize($"Received {receivedBytes} bytes from Client!",
            $"Received {receivedBytes} bytes from {clientEp}.");

        try
        {
            packetManager.HandlePacket(data, receivedBytes, clientEp);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling packet from {clientEp}: {ex}");
        }
    }

    private void OnClientRemoved(ClientInfo client)
    {
        var leavePacket = new PlayerLeavePacket
        {
            Type = PacketType.BroadcastPlayerLeave,
            PlayerId = client.PlayerId
        };

        SendToAll(leavePacket);

        SrLogger.LogMessage($"Player left broadcast sent for: {client.PlayerId}");
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

    internal void Close()
    {
        if (!NetworkManager.IsRunning)
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
        MultiplayerUI.Instance.RegisterSystemMessage("You closed the server!", $"SYSTEM_CLOSE_SERVER_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageClose);

        try
        {
            timeoutTimer?.Dispose();
            timeoutTimer = null;

            var closePacket = new ClosePacket();

            try
            {
                SendToAll(closePacket);
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to broadcast server close: {ex}");
            }

            foreach (var player in PlayerManager.GetAllPlayers())
            {
                var playerId = player.PlayerId;
                if (!PlayerObjects.TryGetValue(playerId, out var playerObject))
                    continue;

                if (playerObject != null)
                {
                    Object.Destroy(playerObject);
                    SrLogger.LogPacketSize($"Destroyed player object for {playerId}");
                }
                PlayerObjects.Remove(playerId);
            }

            PacketDeduplication.Clear();
            ClientManager.Clear();
            PlayerManager.Clear();
            NetworkManager.Stop();
            NetworkStringPool.Clear();
            ApiHandlers.ClearNetIds();

            SrLogger.LogMessage("Server closed");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during server shutdown: {ex}");
        }
    }

    internal void SendToClient<T>(T packet, IPEndPoint endPoint) where T : IPacket
        => PrepareAndSendToClient(packet, packet.Reliability, packet.Channel, endPoint, SerialiseInternalPacket<T>.Serialiser);

    /// <summary>
    /// Sends a custom packet to a specific client endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    /// <param name="endPoint">The endpoint of the client to receive the packet.</param>
    public void SendDataToClient<T>(T data, IPEndPoint endPoint) where T : ICustomPacket
    {
        if (!ApiHandlers.CurrentNetIdMapping2.TryGetValue(data.GetType(), out var modId))
        {
            SrLogger.LogWarning($"Cannot send API packet: No ModId registered for custom packet type {data.GetType().FullName}.");
            return;
        }

        var apiHeader = new ApiPacket(data.Reliability, data.Channel, modId);
        PrepareAndSendToClient((apiHeader, data), apiHeader.Reliability, apiHeader.Channel, endPoint, SerialiseApiPacket<T>.Serialiser);
    }

    /// <summary>
    /// Sends a custom packet to a specific client.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    /// <param name="client">The client info of the recipient.</param>
    public void SendDataToClient<T>(T data, ClientInfo client) where T : ICustomPacket
        => SendDataToClient(data, client.EndPoint);

    private void PrepareAndSendToClient<T>(T state, PacketReliability reliability, NetworkChannel channel,
        IPEndPoint endPoint, PacketWriterDelegate<T> writeAction)
    {
        using var writer = PacketWriter.Borrow();

        try
        {
            writeAction(writer, state);
            var data = writer.ToSpan();

            NetworkManager.Send(data, endPoint, reliability, channel);

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to client at {endPoint}.");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send packet to client {endPoint}: {ex}");
        }
    }

    internal void SendToAll<T>(T packet) where T : IPacket
        => PrepareAndSendToAll(packet, packet.Reliability, packet.Channel, SerialiseInternalPacket<T>.Serialiser);

    /// <summary>
    /// Broadcasts a custom packet to all connected clients.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    public void SendDataToAll<T>(T data) where T : ICustomPacket
    {
        if (!ApiHandlers.CurrentNetIdMapping2.TryGetValue(data.GetType(), out var modId))
        {
            SrLogger.LogWarning($"Cannot send API packet: No ModId registered for custom packet type {data.GetType().FullName}.");
            return;
        }

        var apiHeader = new ApiPacket(data.Reliability, data.Channel, modId);
        PrepareAndSendToAll((apiHeader, data), apiHeader.Reliability, apiHeader.Channel, SerialiseApiPacket<T>.Serialiser);
    }

    private void PrepareAndSendToAll<T>(T state, PacketReliability reliability, NetworkChannel channel,
        PacketWriterDelegate<T> writeAction)
    {
        using var writer = PacketWriter.Borrow();

        try
        {
            writeAction(writer, state);
            var data = writer.ToSpan();

            var endpoints = ClientManager.GetAllClients().Select(c => c.EndPoint);
            NetworkManager.Broadcast(data, endpoints, reliability, channel);

            SrLogger.LogPacketSize($"Broadcasted {data.Length} bytes to all clients.");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to broadcast packet to all: {ex}");
        }
    }

    /// <summary>
    /// Broadcasts a custom packet to all clients except the specified one.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    /// <param name="excludedClientInfo">The client info string to exclude from the broadcast.</param>
    public void SendDataToAllExcept<T>(T data, string excludedClientInfo) where T : ICustomPacket
    {
        if (!ApiHandlers.CurrentNetIdMapping2.TryGetValue(data.GetType(), out var modId))
        {
            SrLogger.LogWarning($"Cannot send API packet: No ModId registered for custom packet type {data.GetType().FullName}.");
            return;
        }

        var apiHeader = new ApiPacket(data.Reliability, data.Channel, modId);
        PrepareAndSendToAllExcept((apiHeader, data), apiHeader.Reliability, apiHeader.Channel, SerialiseApiPacket<T>.Serialiser, excludedClientInfo);
    }

    /// <summary>
    /// Broadcasts a custom packet to all clients except the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet to send.</typeparam>
    /// <param name="data">The packet data to send.</param>
    /// <param name="excludeEndPoint">The endpoint to exclude from the broadcast.</param>
    public void SendDataToAllExcept<T>(T data, IPEndPoint? excludeEndPoint) where T : ICustomPacket
    {
        var clientInfo = $"{excludeEndPoint?.Address}:{excludeEndPoint?.Port}";
        SendDataToAllExcept(data, clientInfo);
    }

    internal void SendToAllExcept<T>(T packet, string excludedClientInfo) where T : IPacket
        => PrepareAndSendToAllExcept(packet, packet.Reliability, packet.Channel, SerialiseInternalPacket<T>.Serialiser, excludedClientInfo);

    internal void SendToAllExcept<T>(T packet, IPEndPoint? excludeEndPoint) where T : IPacket
    {
        var clientInfo = $"{excludeEndPoint?.Address}:{excludeEndPoint?.Port}";
        SendToAllExcept(packet, clientInfo);
    }

    private void PrepareAndSendToAllExcept<T>(T state, PacketReliability reliability, NetworkChannel channel,
        PacketWriterDelegate<T> writeAction, string excludedClientInfo)
    {
        using var writer = PacketWriter.Borrow();

        try
        {
            writeAction(writer, state);
            var data = writer.ToSpan();

            var sentCount = 0;

            foreach (var client in ClientManager.GetAllClients())
            {
                if (client.GetClientInfo() == excludedClientInfo)
                    continue;

                NetworkManager.Send(data, client.EndPoint, reliability, channel);
                sentCount++;
            }

            SrLogger.LogPacketSize($"Broadcasted {data.Length} bytes to {sentCount} client(s).");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to broadcast packet: {ex}");
        }
    }

    // internal int GetPendingReliablePackets() => NetworkManager.GetPendingReliablePackets();
}