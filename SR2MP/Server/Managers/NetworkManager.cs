using System.Net;
using System.Net.Sockets;

namespace SR2MP.Server.Managers;

public class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Thread? receiveThread;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    public void Start(int port)
    {
        if (isRunning)
        {
            SrLogger.LogMessage("Server is already running!", SrLogger.LogTarget.Both);
            return;
        }

        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            SrLogger.LogMessage($"Server started on port: {port}",
                $"Server started {IPAddress.Any}: {port}");

            receiveThread = new Thread(ReceiveLoop);
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

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    OnDataReceived?.Invoke(data, remoteEP);
                    SrLogger.LogMessage($"Received {data.Length} bytes",
                        $"Received {data.Length} bytes from {remoteEP}");
                }
            }
            catch (SocketException)
            {
                if (!isRunning)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ReceiveLoop error: {ex}",  SrLogger.LogTarget.Both);
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send data: Server not running!",  SrLogger.LogTarget.Both);
            return;
        }

        try
        {
            udpClient.Send(data, data.Length, endPoint);
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
                if (!receiveThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SrLogger.LogWarning("Reveive thread did not stop gracefully", SrLogger.LogTarget.Both);
                }
            }

            SrLogger.LogMessage("Server stopped", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error stopping Server: {ex}", SrLogger.LogTarget.Both);
        }
    }
}