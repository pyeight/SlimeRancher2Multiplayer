using System.Net;
using System.Net.Sockets;
using SR2MP.Shared.Managers;
using Thread = Il2CppSystem.Threading.Thread;

namespace SR2MP.Server.Managers;

public sealed class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    // Overload to allow IPv6 configuration
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
                // IPv6 with IPv4 fallback
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                udpClient.Client.DualMode = true;
                udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                SrLogger.LogMessage($"Server started in dual mode (IPv6 + IPv4) on port: {port}", SrLogTarget.Both);
            }
            else
            {
                // IPv4 only
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                SrLogger.LogMessage($"Server started in IPv4 mode on port: {port}", SrLogTarget.Both);
            }

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
                SrLogger.LogPacketSize($"Received {data.Length} bytes",
                    $"Received {data.Length} bytes from {remoteEp}");
            }
            catch (SocketException)
            {
                // never happens, no timeout set
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ReceiveLoop error: {ex}", SrLogTarget.Both);
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send data: Server not running!", SrLogTarget.Both);
            return;
        }

        try
        {
            SrLogger.LogPacketSize($"Sending {data.Length} bytes to client..",
                $"Sending {data.Length} bytes to {endPoint}..");

            var splitData = PacketChunkManager.SplitPacket(data);
            foreach (var chunk in splitData)
            {
                udpClient?.Send(chunk, chunk.Length, endPoint);
            }

            SrLogger.LogPacketSize($"Sent {data.Length} bytes to client in {splitData.Length} chunk(s).",
                $"Sent {data.Length} bytes to {endPoint} in {splitData.Length} chunk(s).");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send data to Client: {ex}",
                $"Failed to send data to {endPoint}: {ex}");
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