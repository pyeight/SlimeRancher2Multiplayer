using System.Net;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SR2MP.Packets.Utils;

namespace SR2MP.Api;

/// <summary>
/// Provides utility methods for sending custom network packets.
/// </summary>
[PublicApi]
public static class PacketSender
{
    /// <summary>
    /// Sends a custom packet from the client to the server.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="packet">The packet instance to send.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendData<T>(T packet) where T : ICustomPacket
        => Main.Client.SendData(packet);

    /// <summary>
    /// Broadcasts a custom packet from the server to all connected clients.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="packet">The packet instance to broadcast.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendDataToAll<T>(T packet) where T : ICustomPacket
        => Main.Server.SendDataToAll(packet);

    /// <summary>
    /// Broadcasts a custom packet from the server to all connected clients, optionally excluding a specific endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="packet">The packet instance to broadcast.</param>
    /// <param name="excludeEndPoint">The client endpoint to exclude from the broadcast, or null to send to everyone.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendDataToAllExcept<T>(T packet, IPEndPoint? excludeEndPoint) where T : ICustomPacket
        => Main.Server.SendDataToAllExcept(packet, excludeEndPoint);

    /// <summary>
    /// Sends a custom packet from the server directly to a specific client.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="packet">The packet instance to send.</param>
    /// <param name="clientEp">The target client's endpoint.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendDataToClient<T>(T packet, IPEndPoint clientEp) where T : ICustomPacket
        => Main.Server.SendDataToClient(packet, clientEp);

    /// <summary>
    /// Sends a custom packet to either the host's machine, or to all clients' machines.
    /// </summary>
    /// <typeparam name="T">The type of the custom packet.</typeparam>
    /// <param name="packet">The packet to send.</param>
    public static void SendDataToAllOrServer<T>(T packet) where T : ICustomPacket
    {
        if (Main.Client.IsConnected)
            Main.Client.SendData(packet);

        if (Main.Server.IsRunning)
            Main.Server.SendDataToAll(packet);
    }
}