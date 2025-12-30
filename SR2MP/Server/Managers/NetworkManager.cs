using System.Net;
using System.Net.Sockets;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Managers;

public sealed class NetworkManager
{
    private UdpClient? udpClient;
    private volatile bool isRunning;
    private Il2CppSystem.Threading.Thread? receiveThread;

    public event Action<byte[], string>? OnDataReceived;

    public bool IsRunning => isRunning;

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
                udpClient = new UdpClient(AddressFamily.InterNetworkV6);
                udpClient.Client.DualMode = true;
                udpClient.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            else
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            }

            udpClient.Client.ReceiveTimeout = 0;
            isRunning = true;
            StartReceiveThread();
            
            SrLogger.LogMessage($"Server started on port {port}", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to start Server: {ex}", SrLogger.LogTarget.Both);
            throw;
        }
    }

    private void StartReceiveThread()
    {
        receiveThread = new Il2CppSystem.Threading.Thread(new Action(ReceiveLoop));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
        while (isRunning && udpClient != null)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                if (data.Length > 0)
                {
                    string senderId = remoteEP.ToString();
                    OnDataReceived?.Invoke(data, senderId);
                }
            }
            catch (SocketException)
            {
                if (!isRunning) return;
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Server ReceiveLoop error: {ex}");
            }
        }
    }

    public void Send(byte[] data, IPEndPoint targetEndPoint)
    {
        if (udpClient == null || !isRunning)
        {
            SrLogger.LogWarning("Cannot send data: Server not running!");
            return;
        }

        try
        {
            udpClient.Send(data, data.Length, targetEndPoint);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Failed to send data to {targetEndPoint}: {ex}");
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
            udpClient = null;

            SrLogger.LogMessage("Server stopped", SrLogger.LogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error stopping Server: {ex}", SrLogger.LogTarget.Both);
        }
    }
}
