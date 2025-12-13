namespace SR2MP.Packets.Utils;

public enum PacketType : byte
{   // Type                       // Hierarchy                                    // Exception                          // Use Case
    Connect = 0,                  // Client -> Server                                                                   Try to connect to Server
    ConnectAck = 1,               // Server -> Client                                                                   Initiate Player Join
    Close = 2,                    // Server -> All Clients                                                              Broadcast Server Close
    PlayerJoin = 3,               // Client -> Server                                                                   Add Player
    BroadcastPlayerJoin = 4,      // Server -> All Clients                        (except client that joins)            Add Player on other Clients
    PlayerLeave = 5,              // Client -> Server                                                                   Remove Player
    BroadcastPlayerLeave = 6,     // Server -> All Clients                        (except client that left)             Remove Player on other Clients
    PlayerUpdate = 7,             // Client -> Server                                                                   Update Player
    BroadcastPlayerUpdate = 8,    // Server -> All Clients                        (except client that updates)          Update Player on other Clients
    ChatMessage = 9,              // Client -> Server                                                                   Chat message
    BroadcastChatMessage = 10,    // Server -> All clients                                                              Chat message on other Clients
    Heartbeat = 16,               // Client -> Server                                                                   Check if Clients are alive
    HeartbeatAck = 17,            // Server -> Client                                                                   Automatically time the Clients out if the Server crashes
}