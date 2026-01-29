using System.Net;
using System.Net.Sockets;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Managers;

public sealed class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;

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

            isRunning = true;

            receiveThread = new Thread(new Action(ReceiveLoop));
            receiveThread.IsBackground = true;
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

        IPEndPoint remoteEp = new IPEndPoint(IPAddress.IPv6Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEp);

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

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send: Server not running!", SrLogTarget.Both);
            return;
        }

        try
        {
            var chunks = PacketChunkManager.SplitPacket(data, out ushort packetId);
            
            // Send all chunks
            foreach (var chunk in chunks)
            {
                udpClient.Send(chunk, chunk.Length, endPoint);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Send failed to {endPoint}: {ex}", SrLogTarget.Both);
        }
    }
    
    // Broadcast to multiple endpoints efficiently
    public void Broadcast(byte[] data, IEnumerable<IPEndPoint> endpoints)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot broadcast: Server not running!", SrLogTarget.Both);
            return;
        }

        try
        {
            // Split once, send to many
            var chunks = PacketChunkManager.SplitPacket(data, out ushort packetId);
            
            foreach (var endpoint in endpoints)
            {
                foreach (var chunk in chunks)
                {
                    udpClient.Send(chunk, chunk.Length, endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Broadcast failed: {ex}", SrLogTarget.Both);
        }
    }

    public void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;

        try
        {
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
}