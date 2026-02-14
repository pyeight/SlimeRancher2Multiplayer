using System.Net;
using System.Net.Sockets;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Managers;

public sealed class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;
    private ReliabilityManager? reliabilityManager;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    public void Start(int port, bool enableIPv6 = true)
    {
        if (isRunning)
        {
            SrLogger.LogMessage("Server is already running!", SrLogTarget.Both);
            return;
        }

        try
        {
            if (enableIPv6)
            {
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                udpClient.Client.DualMode = true;
                udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                SrLogger.LogMessage($"Server started in dual mode (IPv6 + IPv4) on port: {port}", SrLogTarget.Both);
            }
            else
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                SrLogger.LogMessage($"Server started in IPv4 mode on port: {port}", SrLogTarget.Both);
            }

            udpClient.Client.ReceiveBufferSize = 512 * 1024;
            udpClient.Client.SendBufferSize = 512 * 1024;
            udpClient.Client.ReceiveTimeout = 0;

            reliabilityManager = new ReliabilityManager(SendRaw);
            reliabilityManager.Start();

            isRunning = true;

            receiveThread = new Thread(new Action(ReceiveLoop))
            {
                IsBackground = true
            };
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start Server: {ex}", SrLogTarget.Both);
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("Server is null in ReceiveLoop!", SrLogTarget.Both);
            return;
        }

        SrLogger.LogMessage("Server ReceiveLoop started!", SrLogTarget.Both);

        var remoteEp = new IPEndPoint(IPAddress.IPv6Any, 0);

        while (isRunning)
        {
            try
            {
                var data = udpClient.Receive(ref remoteEp);

                if (data.Length == 0)
                    continue;

                OnDataReceived?.Invoke(data, remoteEp);
            }
            catch (SocketException)
            {
                // never happens, no timeout set
            }
            catch (Exception ex)
            {
                if (isRunning)
                    SrLogger.LogError($"ReceiveLoop error: {ex}", SrLogTarget.Both);
            }
        }

        SrLogger.LogMessage("Server ReceiveLoop stopped", SrLogTarget.Both);
    }

    public void Send(ReadOnlySpan<byte> data, IPEndPoint endPoint, PacketReliability? reliability = null)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send: Server not running!", SrLogTarget.Both);
            return;
        }

        try
        {
            var packetReliability = reliability ?? PacketReliability.Unreliable;
            ushort sequenceNumber = 0;

            if (packetReliability == PacketReliability.ReliableOrdered)
            {
                sequenceNumber = reliabilityManager?.GetNextSequenceNumber(data[0]) ?? 0;
            }

            var chunks = PacketChunkManager.SplitPacket(data, packetReliability, sequenceNumber, out var packetId);

            if (packetReliability != PacketReliability.Unreliable)
            {
                reliabilityManager?.TrackPacket(chunks, endPoint, packetId, data[0], packetReliability, sequenceNumber);
            }

            foreach (var chunk in chunks)
            {
                SendRaw(chunk, endPoint);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Send failed to {endPoint}: {ex}", SrLogTarget.Both);
        }
    }

    // Broadcast to multiple endpoints efficiently
    public void Broadcast(ReadOnlySpan<byte> data, IEnumerable<IPEndPoint> endpoints, PacketReliability? reliability = null)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot broadcast: Server not running!", SrLogTarget.Both);
            return;
        }

        try
        {
            var finalReliability = reliability ?? PacketReliability.Unreliable;
            ushort sequenceNumber = 0;

            if (finalReliability == PacketReliability.ReliableOrdered)
            {
                sequenceNumber = reliabilityManager?.GetNextSequenceNumber(data[0]) ?? 0;
            }

            // Split once, send to many
            var chunks = PacketChunkManager.SplitPacket(data, finalReliability, sequenceNumber, out var packetId);

            foreach (var endpoint in endpoints)
            {
                // Track for reliability if needed
                if (finalReliability != PacketReliability.Unreliable)
                {
                    reliabilityManager?.TrackPacket(chunks, endpoint, packetId, data[0], finalReliability, sequenceNumber);
                }

                foreach (var chunk in chunks)
                {
                    SendRaw(chunk, endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Broadcast failed: {ex}", SrLogTarget.Both);
        }
    }

    // Sends raw data without reliability tracking (used for resends or internally)
    private void SendRaw(byte[] data, IPEndPoint endPoint)
    {
        udpClient?.Send(data, data.Length, endPoint);
    }

    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        reliabilityManager?.HandleAck(sender, packetId, packetType);
    }

    // Check if ordered packet should be processed
    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType)
    {
        return reliabilityManager?.ShouldProcessOrderedPacket(sender, sequenceNumber, packetType) ?? true;
    }

    public void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;

        try
        {
            reliabilityManager?.Stop();
            udpClient?.Close();

            if (receiveThread is { IsAlive: true })
            {
                SrLogger.LogWarning("Receive thread did not stop gracefully", SrLogTarget.Both);
            }

            SrLogger.LogMessage("Server stopped", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error stopping Server: {ex}", SrLogTarget.Both);
        }
    }

    public int GetPendingReliablePackets() => reliabilityManager?.GetPendingPacketCount() ?? 0;
}