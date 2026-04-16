using System.Buffers;
using System.Net;
using System.Net.Sockets;
using SR2MP.Packets.Utils;
using SR2MP.Server.Models;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Managers;

internal sealed class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;
    private ReliabilityManager? reliabilityManager;

    public event Action<byte[], int, IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    public void Start(int port, bool enableIPv6 = true)
    {
        if (isRunning)
        {
            SrLogger.LogMessage("Server is already running!");
            return;
        }

        try
        {
            if (enableIPv6)
            {
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                udpClient.Client.DualMode = true;
                udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                SrLogger.LogMessage($"Server started in dual mode (IPv6 + IPv4) on port: {port}");
            }
            else
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                SrLogger.LogMessage($"Server started in IPv4 mode on port: {port}");
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
            SrLogger.LogError($"Failed to start Server: {ex}");
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("Server is null in ReceiveLoop!");
            return;
        }

        SrLogger.LogMessage("Server ReceiveLoop started!");

        EndPoint remoteEp = new IPEndPoint(IPAddress.IPv6Any, 0);
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(2048);

        try
        {
            while (isRunning)
            {
                try
                {
                    var receivedBytes = udpClient.Client.ReceiveFrom(receiveBuffer, ref remoteEp);

                    if (receivedBytes > 0)
                        OnDataReceived?.Invoke(receiveBuffer, receivedBytes, (IPEndPoint)remoteEp);
                }
                catch (SocketException)
                {
                    // never happens, no timeout set
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        SrLogger.LogError($"ReceiveLoop error: {ex}");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
            SrLogger.LogMessage("Server ReceiveLoop stopped");
        }
    }

    public void Send(ReadOnlySpan<byte> data, IPEndPoint endPoint, PacketReliability? reliability = null,
        NetworkChannel channel = NetworkChannel.Default)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send: Server not running!");
            return;
        }

        try
        {
            var packetReliability = reliability ?? PacketReliability.Unreliable;
            ushort sequenceNumber = 0;

            if (packetReliability.HasFlag(PacketReliability.Ordered))
                sequenceNumber = reliabilityManager?.GetNextSequenceNumber(channel, data[0], endPoint) ?? 0;

            var splitResult = PacketChunkManager.SplitPacket(data, packetReliability, channel, sequenceNumber, out var packetId);

            if (packetReliability.HasFlag(PacketReliability.Reliable))
                reliabilityManager?.TrackPacket(splitResult, endPoint, packetId, data[0], packetReliability);

            for (var i = 0; i < splitResult.Count; i++)
                SendRaw(splitResult.Chunks[i], endPoint);

            if (!packetReliability.HasFlag(PacketReliability.Reliable))
                splitResult.Dispose();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Send failed to {endPoint}: {ex}");
        }
    }

    public void Broadcast(ReadOnlySpan<byte> data, ICollection<ClientInfo> endPoints,
        PacketReliability? reliability = null, NetworkChannel channel = NetworkChannel.Important)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot broadcast: Server not running!");
            return;
        }

        try
        {
            var packetReliability = reliability ?? PacketReliability.Unreliable;

            if (packetReliability.HasFlag(PacketReliability.Ordered))
            {
                foreach (var endPoint in endPoints)
                    Send(data, endPoint.EndPoint, reliability, channel);

                return;
            }

            var splitResult = PacketChunkManager.SplitPacket(data, packetReliability, channel, 0, out var packetId);

            foreach (var endPoint in endPoints)
            {
                if (packetReliability.HasFlag(PacketReliability.Reliable))
                    reliabilityManager?.TrackPacket(splitResult, endPoint.EndPoint, packetId, data[0], packetReliability);

                for (var i = 0; i < splitResult.Count; i++)
                    SendRaw(splitResult.Chunks[i], endPoint.EndPoint);
            }

            if (packetReliability == PacketReliability.Unreliable)
                splitResult.Dispose();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Broadcast failed: {ex}");
        }
    }

    // Sends raw data without reliability tracking (used for resends or internally)
    private void SendRaw(ArraySegment<byte> data, IPEndPoint endPoint)
    {
        if (data.Array != null)
            udpClient?.Client.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, endPoint);
    }

    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
        => reliabilityManager?.HandleAck(sender, packetId, packetType);

    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType,
        NetworkChannel channel, PacketReliability reliability, Action? processAction = null)
            => reliabilityManager?.ShouldProcessOrderedPacket(sender, sequenceNumber, packetType, channel, reliability, processAction) ?? true;

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
                SrLogger.LogWarning("Receive thread did not stop gracefully");
            }

            SrLogger.LogMessage("Server stopped");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error stopping Server: {ex}");
        }
    }

    public int GetPendingReliablePackets() => reliabilityManager?.GetPendingPacketCount() ?? 0;
}