using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SR2MP;

public sealed class Server
{
    public enum PacketType : byte
    {   // Type                 // Hierachy                                     // Exception                            // Use Case
        Connect = 0,            // Client -> Server                                                                     Try to connect to Server
        ConnectAck = 1,         // Server -> Client                                                                     Initiate Player Join
        Close = 2,              // Server -> All Clients                                                                Broadcast Server Close
        PlayerJoin = 3,         // Server -> All Clients                        (except client that joins)              Add Player
        PlayerLeave = 4,        // Server -> All Clients                        (except client that left)               Remove Player
        PlayerUpdate = 5,       // Client -> Server -> All Clients              (except updater)                        Update Player
        Heartbeat = 8,          // Client -> Server                                                                     Check if Clients are alive
        HeartbeatAck = 9,       // Server -> Client                                                                     Automatically time the Clients out if the Server crashes
    }

    private UdpClient? server;
    private Thread? receiverThread;
    // volatile necessary for multi-threading
    private volatile bool running;
    // ConcurrentDictionary is a Dictionary but safe for multi-threading
    private readonly ConcurrentDictionary<string, ClientInfo> clients = new ConcurrentDictionary<string, ClientInfo>();

    public void Start(int port)
    {
        if (running)
        {
            SrLogger.LogSensitive("Server is already running!");
            SrLogger.Log("Server is already running!");
            return;
        }

        try
        {
            server = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            running = true;

            SrLogger.LogSensitive($"Server started on port {port}");
            SrLogger.Log($"Server started on port {port}");

            receiverThread = new Thread(ReceiveLoop);
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to start server: {ex}");
            SrLogger.Error($"Failed to start server: {ex}");
        }
    }

    private void ReceiveLoop()
    {
        if (server == null)
        {
            SrLogger.ErrorSensitive("Server is null!");
            SrLogger.Error("Server is null!");
            return;
        }

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = server.Receive(ref remoteEP);

                if (data.Length < 1)
                    continue;

                string clientInfo = $"{remoteEP.Address}:{remoteEP.Port}";
                PacketType type = (PacketType)data[0];

                switch (type)
                {
                    case PacketType.Connect:
                        // HandleConnect(data, remoteEP, clientInfo);
                        break;

                    case PacketType.Heartbeat:
                        HandleHeartbeat(clientInfo);
                        break;

                    default:
                        SrLogger.WarnSensitive($"Unknown packet type: {type} from {clientInfo}");
                        SrLogger.Warn($"Unknown packet type: {type} from {clientInfo}");
                        break;
                }
            }
            catch (SocketException)
            {
                if (!running)
                    return;
            }
            catch (Exception ex)
            {
                SrLogger.ErrorSensitive($"ReceiveLoop Error: {ex}");
                SrLogger.Error($"ReceiveLoop Error: {ex}");
            }
        }
    }

    private void Send(byte[] data, IPEndPoint clientEP)
    {
        if (server == null) return;

        try
        {
            server.Send(data, data.Length, clientEP);
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Failed to send data to {clientEP}: {ex}");
            SrLogger.Error($"Failed to send data to client: {ex}");
        }
    }

    private void Send(byte[] data, string clientInfo)
    {
        if (clients.TryGetValue(clientInfo, out ClientInfo? client))
        {
            Send(data, client.EndPoint);
        }
        else
        {
            SrLogger.WarnSensitive($"Attempted to send to unknown client: {clientInfo}");
            SrLogger.Warn($"Attempted to send to unknown client!");
        }
    }

    public class ClientInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string PlayerId { get; set; }

        public ClientInfo(IPEndPoint endPoint, string playerId = "")
        {
            EndPoint = endPoint;
            LastHeartbeat = DateTime.UtcNow;
            PlayerId = playerId;
        }

        public void UpdateHeartbeat() => LastHeartbeat = DateTime.UtcNow;

        public bool IsTimedOut() => (DateTime.UtcNow - LastHeartbeat).TotalSeconds > 30;
    }

    public void Close()
    {
        if (!running)
            return;

        running = false;

        try
        {
            byte[] closePacket = new byte[] { (byte)PacketType.Close };
            foreach (var client in clients)
            {
                try
                {
                    Send(closePacket, client.Value.EndPoint);
                }
                catch (Exception ex)
                {
                    SrLogger.WarnSensitive($"Failed to send close packet to client: {client.Key}: {ex}");
                    SrLogger.Warn($"Failed to notify client of server shutdown!");
                }
            }

            clients.Clear();

            server?.Close();

            if (receiverThread != null && receiverThread.IsAlive)
            {
                if (!receiverThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SrLogger.WarnSensitive($"Failed to stop receiver thread!");
                    SrLogger.Warn("Failed to stop receiver thread!");
                }
            }

            SrLogger.LogSensitive("Server closed");
            SrLogger.Log("Server closed");
        }
        catch (Exception ex)
        {
            SrLogger.ErrorSensitive($"Error during server shutdown: {ex}");
            SrLogger.Error($"Error during server shutdown: {ex}");
        }
    }

    private void HandleHeartbeat(string clientInfo)
    {
        if (clients.TryGetValue(clientInfo, out ClientInfo? client))
        {
            client.UpdateHeartbeat();

            byte[] acknowledgePacket = new byte[] { (byte)PacketType.HeartbeatAck };
            Send(acknowledgePacket, clientInfo);

            SrLogger.LogSensitive($"Heartbeat received from {clientInfo}");
        }
        else
        {
            SrLogger.WarnSensitive($"Received Heartbeat from unknown client: {clientInfo}");
            SrLogger.Warn("Received Heartbeat from unknown client!");
        }
    }
}