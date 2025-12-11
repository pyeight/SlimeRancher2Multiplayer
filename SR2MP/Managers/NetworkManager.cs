using System.Net;
using System.Net.Sockets;

namespace SR2MP.Managers;

public sealed class NetworkManager
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
            SrLogger.LogMessageBoth("Server is already running!");
            return;
        }

        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            SrLogger.LogMessageBoth($"Server started on port: {port}");

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorBoth($"Failed to start Server: {ex}");
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            SrLogger.LogErrorBoth("Server is null in ReceiveLoop!");
            return;
        }

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                if (data.Length > 0)
                {
                    OnDataReceived?.Invoke(data, remoteEP);
                }
            }
            catch (SocketException)
            {
                if (!isRunning)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.LogErrorBoth($"ReceiveLoop error: {ex}");
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogErrorBoth("Cannot send data: Server not running!");
            return;
        }

        try
        {
            udpClient.Send(data, data.Length, endPoint);
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorSensitive($"Failed to send data to {endPoint}: {ex}");
            SrLogger.LogError($"Failed to send data to Client: {ex}");
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

            if (receiveThread?.IsAlive == true && !receiveThread.Join(TimeSpan.FromSeconds(2)))
            {
                SrLogger.LogWarningBoth("Receive thread did not stop gracefully");
            }

            SrLogger.LogMessageBoth("Server stopped");
        }
        catch (Exception ex)
        {
            SrLogger.LogErrorBoth($"Error stopping Server: {ex}");
        }
    }
}