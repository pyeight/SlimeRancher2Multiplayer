using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server;

public sealed class SR2MPServer
{
    public readonly NetworkManager NetworkManager;
    public readonly ClientManager ClientManager;
    public readonly ReSyncManager ReSyncManager;

    private readonly ServerPacketManager packetManager;

    private Timer? timeoutTimer;

    // Just here so that the port is viewable.
    public int Port { get; private set; }
    public string PlayerId { get; private set; } = string.Empty;

    public event Action? OnServerStarted;

    public SR2MPServer()
    {
        NetworkManager = new NetworkManager();
        ClientManager = new ClientManager();
        ReSyncManager = new ReSyncManager();
        packetManager = new ServerPacketManager(NetworkManager, ClientManager);

        NetworkManager.OnDataReceived += OnDataReceived;
        ClientManager.OnClientRemoved += OnClientRemoved;
    }

    // public int GetClientCount() => ClientManager.ClientCount;

    public bool IsRunning() => NetworkManager.IsRunning;

    public void Start(int port, bool enableIPv6)
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
            PlayerId = devMode ? "PLAYER_TEST_MODE" : PlayerIdGenerator.GeneratePersistentPlayerId();

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

    public void Close()
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

            SrLogger.LogMessage("Server closed");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during server shutdown: {ex}");
        }
    }

    public void SendToClient<T>(T packet, IPEndPoint endPoint) where T : IPacket
    {
        var writer = PacketWriter.Borrow();

        try
        {
            writer.WritePacket(packet);
            NetworkManager.Send(writer.ToSpan(), endPoint, packet.Reliability);
        }
        finally
        {
            PacketWriter.Return(writer);
        }
    }

    public void SendToClient<T>(T packet, ClientInfo client) where T : IPacket
    {
        SendToClient(packet, client.EndPoint);
    }

    public void SendToAll<T>(T packet) where T : IPacket
    {
        var writer = PacketWriter.Borrow();

        try
        {
            writer.WritePacket(packet);
            var endpoints = ClientManager.GetAllClients().Select(c => c.EndPoint);
            NetworkManager.Broadcast(writer.ToSpan(), endpoints, packet.Reliability);
        }
        finally
        {
            PacketWriter.Return(writer);
        }
    }

    public void SendToAllExcept<T>(T packet, string excludedClientInfo) where T : IPacket
    {
        var writer = PacketWriter.Borrow();

        try
        {
            writer.WritePacket(packet);
            var data = writer.ToSpan();

            foreach (var client in ClientManager.GetAllClients())
            {
                if (client.GetClientInfo() != excludedClientInfo)
                    NetworkManager.Send(data, client.EndPoint, packet.Reliability);
            }
        }
        finally
        {
            PacketWriter.Return(writer);
        }
    }

    public void SendToAllExcept<T>(T packet, IPEndPoint? excludeEndPoint) where T : IPacket
    {
        var clientInfo = $"{excludeEndPoint?.Address}:{excludeEndPoint?.Port}";
        SendToAllExcept(packet, clientInfo);
    }

    // public int GetPendingReliablePackets() => NetworkManager.GetPendingReliablePackets();
}