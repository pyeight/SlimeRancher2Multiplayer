using System.Net;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SR2MP.Packets.Utils;

namespace SR2MP.Api;

/// <summary>
/// Represents a base handler for processing custom network packets on either the client or server.
/// </summary>
/// <typeparam name="T">The type of the custom packet to handle. Must implement <see cref="ICustomPacket"/> and have a parameterless constructor.</typeparam>
[PublicAPI]
public abstract class BaseCustomPacketHandler<T> : IClientPacketHandler, IServerPacketHandler where T : ICustomPacket, new()
{
    /// <summary>
    /// Gets or sets a value indicating whether this handler is currently executing on the server side.
    /// </summary>
    public bool IsServerSide { protected get; set; }

    /// <summary>
    /// Handles an incoming packet from the server. This method is invoked on the client side.
    /// </summary>
    /// <param name="reader">The reader containing the packet data.</param>
    public void Handle(PacketReader reader)
    {
        if (!IsServerSide)
            ProcessPacket(reader, null);
    }

    /// <summary>
    /// Handles an incoming packet from a client. This method is invoked on the server side.
    /// </summary>
    /// <param name="reader">The reader containing the packet data.</param>
    /// <param name="clientEp">The endpoint of the client that sent the packet.</param>
    public void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (IsServerSide)
            ProcessPacket(reader, clientEp);
    }

    private void ProcessPacket(PacketReader reader, IPEndPoint? clientEp)
    {
        var packet = reader.ReadCustomPacket<T>();
        var shouldSend = Handle(packet, clientEp);

        if (IsServerSide && shouldSend)
            PacketSender.SendDataToAllExcept(packet, clientEp);
    }

    /// <summary>
    /// Processes the strongly-typed packet data.
    /// </summary>
    /// <param name="packet">The deserialized packet instance.</param>
    /// <param name="selfEp">The endpoint of the sender (if server-side), or null (if client-side).</param>
    /// <returns><c>true</c> if the server should automatically relay this packet to all other clients; otherwise, <c>false</c>.</returns>
    protected abstract bool Handle(T packet, IPEndPoint? selfEp);
}

/// <summary>
/// Provides utility methods for sending custom network packets.
/// </summary>
[PublicAPI]
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
}