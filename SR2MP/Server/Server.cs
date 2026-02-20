using System.Net;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Packets.Player;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server;

public sealed class SR2MPServer
{
    public readonly NetworkManager networkManager;
    public readonly ClientManager clientManager;
    public readonly ReSyncManager reSyncManager;

    private readonly ServerPacketManager packetManager;

    private Timer? timeoutTimer;

    // Just here so that the port is viewable.
    public int Port { get; private set; }

    public event Action? OnServerStarted;

    public SR2MPServer()
    {
        networkManager = new NetworkManager();
        clientManager = new ClientManager();
        reSyncManager = new ReSyncManager();
        packetManager = new ServerPacketManager(networkManager, clientManager);

        networkManager.OnDataReceived += OnDataReceived;
        clientManager.OnClientRemoved += OnClientRemoved;
    }

    public int GetClientCount() => clientManager.ClientCount;

    public bool IsRunning() => networkManager.IsRunning;

    public void Start(int port, bool enableIPv6)
    {
        if (Main.Client.IsConnected)
        {
            SrLogger.LogWarning("You are already connected to a server, restart your game to host your own server");
            return;
        }

        if (networkManager.IsRunning)
        {
            SrLogger.LogMessage("Server is already running!", SrLogTarget.Both);
            return;
        }

        try
        {
            packetManager.RegisterHandlers();
            Application.quitting += new Action(Close);
            networkManager.Start(port, enableIPv6);
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
            SrLogger.LogError($"Failed to start server: {ex}", SrLogTarget.Both);
        }
    }

    private void OnDataReceived(byte[] data, IPEndPoint clientEp)
    {
        SrLogger.LogPacketSize($"Received {data.Length} bytes from Client!",
            $"Received {data.Length} bytes from {clientEp}.");

        try
        {
            packetManager.HandlePacket(data, clientEp);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error handling packet from {clientEp}: {ex}", SrLogTarget.Both);
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
        if (!networkManager.IsRunning)
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
        MultiplayerUI.Instance.RegisterSystemMessage("You closed the server!", $"SYSTEM_CLOSE_HOST_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", MultiplayerUI.SystemMessageClose);

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

            PacketDeduplication.Clear();
            clientManager.Clear();
            playerManager.Clear();
            networkManager.Stop();

            SrLogger.LogMessage("Server closed", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error during server shutdown: {ex}", SrLogTarget.Both);
        }
    }

    public void SendToClient<T>(T packet, IPEndPoint endPoint) where T : IPacket
    {
        var writer = PacketBufferPool.GetWriter();

        try
        {
            writer.WritePacket(packet);
            networkManager.Send(writer.ToSpan(), endPoint, packet.Reliability);
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }

    public void SendToClient<T>(T packet, ClientInfo client) where T : IPacket
    {
        SendToClient(packet, client.EndPoint);
    }

    public void SendToAll<T>(T packet) where T : IPacket
    {
        var writer = PacketBufferPool.GetWriter();

        try
        {
            writer.WritePacket(packet);
            var endpoints = clientManager.GetAllClients().Select(c => c.EndPoint);
            networkManager.Broadcast(writer.ToSpan(), endpoints, packet.Reliability);
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }

    public void SendToAllExcept<T>(T packet, string excludedClientInfo) where T : IPacket
    {
        var writer = PacketBufferPool.GetWriter();

        try
        {
            writer.WritePacket(packet);
            var data = writer.ToSpan();

            foreach (var client in clientManager.GetAllClients())
            {
                if (client.GetClientInfo() != excludedClientInfo)
                    networkManager.Send(data, client.EndPoint, packet.Reliability);
            }
        }
        finally
        {
            PacketBufferPool.Return(writer);
        }
    }

    public void SendToAllExcept<T>(T packet, IPEndPoint? excludeEndPoint) where T : IPacket
    {
        var clientInfo = $"{excludeEndPoint?.Address}:{excludeEndPoint?.Port}";
        SendToAllExcept(packet, clientInfo);
    }

    public int GetPendingReliablePackets() => networkManager.GetPendingReliablePackets();
}