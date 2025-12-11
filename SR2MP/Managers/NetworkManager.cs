using System.Net;
using System.Net.Sockets;

namespace SR2MP.Managers;

public sealed class NetworkManager
{
    private UdpClient? udpClient;
    private Thread? receiveThread;

    private volatile bool isRunning;

    public event Action<byte[], IPEndPoint>? OnDataReceived;

    public bool IsRunning => isRunning;

    public void Start(int port)
    {
        if (isRunning)
        {
            Logger.LogMessage("Server is already running!", Logger.LogTarget.Both);
            return;
        }

        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            isRunning = true;

            Logger.LogMessage($"Server started on port: {port}", Logger.LogTarget.Both);

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to start Server: {ex}", Logger.LogTarget.Both);
            throw;
        }
    }

    private void ReceiveLoop()
    {
        if (udpClient == null)
        {
            Logger.LogError("Server is null in ReceiveLoop!", Logger.LogTarget.Both);
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
                Logger.LogError($"ReceiveLoop error: {ex}", Logger.LogTarget.Both);
            }
        }
    }

    public void Send(byte[] data, IPEndPoint endPoint)
    {
        if (udpClient == null || !isRunning)
        {
            Logger.LogError("Cannot send data: Server not running!", Logger.LogTarget.Both);
            return;
        }

        try
        {
            udpClient.Send(data, data.Length, endPoint);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to send data to Client: {ex}", $"Failed to send data to {endPoint}: {ex}");
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
                Logger.LogWarning("Receive thread did not stop gracefully", Logger.LogTarget.Both);
            }

            Logger.LogMessage("Server stopped", Logger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error stopping Server: {ex}", Logger.LogTarget.Both);
        }
    }
}