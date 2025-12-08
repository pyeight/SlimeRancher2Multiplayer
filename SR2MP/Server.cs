using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SR2MP;

public class Server
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
    private ConcurrentDictionary<string, ClientInfo> clients = new ConcurrentDictionary<string, ClientInfo>();
    
    public void Start(int port)
    {
        if (running)
        {
            SR2MP.Logger.LogSensitive("Server is already running!");
            SR2MP.Logger.Log("Server is already running!");
            return;
        }   
        
        try
        {
            server = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            running = true;

            SR2MP.Logger.LogSensitive($"Server started on port {port}");
            SR2MP.Logger.Log($"Server started on port {port}");

            receiverThread = new Thread(ReceiveLoop);
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }
        catch (Exception ex)
        {
            SR2MP.Logger.ErrorSensitive($"Failed to start server: {ex}");
            SR2MP.Logger.Error($"Failed to start server: {ex}");
        }
    }
    
    private void ReceiveLoop()
    {
        if (server == null)
        {
            SR2MP.Logger.ErrorSensitive("Server is null!");
            SR2MP.Logger.Error("Server is null!");
            return;
        }    
        
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = server.Receive(ref remoteEP);
                if (data.Length < 1) continue;

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
                        SR2MP.Logger.WarnSensitive($"Unknown packet type: {type} from {clientInfo}");
                        SR2MP.Logger.Warn($"Unknown packet type: {type} from {clientInfo}");
                        break;
                }
            }
            catch (SocketException) 
            {
                if (!running) return;
            }
            catch (Exception ex)
            {
                SR2MP.Logger.ErrorSensitive($"ReceiveLoop Error: {ex}");
                SR2MP.Logger.Error($"ReceiveLoop Error: {ex}");
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
            SR2MP.Logger.ErrorSensitive($"Failed to send data to {clientEP}: {ex}");
            SR2MP.Logger.Error($"Failed to send data to client: {ex}");
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
            SR2MP.Logger.WarnSensitive($"Attempted to send to unknown client: {clientInfo}");
            SR2MP.Logger.Warn($"Attempted to send to unknown client!");
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
        
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
        }
        
        public bool IsTimedOut()
        {
            return (DateTime.UtcNow - LastHeartbeat).TotalSeconds > 30;
        }
    }
    
    public void Close()
    {
        if (!running) return;
    
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
                    SR2MP.Logger.WarnSensitive($"Failed to send close packet to client: {client.Key}: {ex}");
                    SR2MP.Logger.Warn($"Failed to notify client of server shutdown!");
                }
            }
            
            clients.Clear();
            
            server?.Close();
            
            if (receiverThread != null && receiverThread.IsAlive)
            {
                if (!receiverThread.Join(TimeSpan.FromSeconds(2)))
                {
                    SR2MP.Logger.WarnSensitive($"Failed to stop receiver thread!");
                    SR2MP.Logger.Warn("Failed to stop receiver thread!");
                }
            }
        
            SR2MP.Logger.LogSensitive("Server closed");
            SR2MP.Logger.Log("Server closed");
        }
        catch (Exception ex)
        {
            SR2MP.Logger.ErrorSensitive($"Error during server shutdown: {ex}");
            SR2MP.Logger.Error($"Error during server shutdown: {ex}");
        }
        
        
    }
    private void HandleHeartbeat(string clientInfo)
    {
        if (clients.TryGetValue(clientInfo, out ClientInfo? client))
        {
            client.UpdateHeartbeat();
            
            byte[] acknowledgePacket = new byte[] { (byte)PacketType.HeartbeatAck };
            Send(acknowledgePacket, clientInfo);
        
            SR2MP.Logger.LogSensitive($"Heartbeat received from {clientInfo}");
        }
        else
        {
            SR2MP.Logger.WarnSensitive($"Received Heartbeat from unknown client: {clientInfo}");
            SR2MP.Logger.Warn("Received Heartbeat from unknown client!");
        }
    }
}