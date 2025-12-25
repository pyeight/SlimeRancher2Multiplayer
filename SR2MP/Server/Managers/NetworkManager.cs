using System.Net;
using System.Net.Sockets;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Managers;

public class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Il2CppSystem.Threading.Thread? receiveThread;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    // Overload to allow IPv6 configuration
    public void Start(int port, bool enableIPv6 = true)
    {
        if (isRunning)
        {
            SrLogger.LogMessage("Server is already running!", SrLogger.LogTarget.Both);
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
                SrLogger.LogMessage($"Server started in dual mode (IPv6 + IPv4) on port: {port}", SrLogger.LogTarget.Both);
            }
            else
            {
                // IPv4 only
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                SrLogger.LogMessage($"Server started in IPv4 mode on port: {port}", SrLogger.LogTarget.Both);
            }

            udpClient.Client.ReceiveTimeout = 0;

            isRunning = true;

            receiveThread = new Il2CppSystem.Threading.Thread(new Action(ReceiveLoop));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start Server: {ex}", SrLogger.LogTarget.Both);
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogError("Server is null in ReceiveLoop!", SrLogger.LogTarget.Both);
            return;
        }

        SrLogger.LogMessage("Server ReceiveLoop started!", SrLogger.LogTarget.Both);

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    OnDataReceived?.Invoke(data, remoteEP);
                    SrLogger.LogPacketSize($"Received {data.Length} bytes",
                        $"Received {data.Length} bytes from {remoteEP}");
                }
            }
            catch (SocketException)
            {
                // never happens, no timeout set
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ReceiveLoop error: {ex}", SrLogger.LogTarget.Both);
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send data: Server not running!", SrLogger.LogTarget.Both);
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

            if (receiveThread != null && receiveThread.IsAlive)
            {
                SrLogger.LogWarning("Receive thread did not stop gracefully", SrLogger.LogTarget.Both);
            }

            SrLogger.LogMessage("Server stopped", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error stopping Server: {ex}", SrLogger.LogTarget.Both);
        }
    }
}