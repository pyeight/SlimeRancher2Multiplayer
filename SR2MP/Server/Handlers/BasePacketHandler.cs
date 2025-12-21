using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

public abstract class BasePacketHandler : IPacketHandler
{
    protected readonly NetworkManager networkManager;
    protected readonly ClientManager clientManager;

    protected BasePacketHandler(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public abstract void Handle(byte[] data, IPEndPoint senderEndPoint);

    private void SendToClient(IPacket packet, IPEndPoint endPoint)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        networkManager.Send(writer.ToArray(), endPoint);
    }

    protected void SendToClient(IPacket packet, ClientInfo client)
    {
        SendToClient(packet, client.EndPoint);
    }

    protected void BroadcastToAll(IPacket packet)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            networkManager.Send(data, client.EndPoint);
        }
    }

    private void BroadcastToAllExcept(IPacket packet, string excludedClientInfo)
    {
        using var writer = new PacketWriter();
        packet.Serialise(writer);
        byte[] data = writer.ToArray();

        foreach (var client in clientManager.GetAllClients())
        {
            if (client.GetClientInfo() != excludedClientInfo)
            {
                networkManager.Send(data, client.EndPoint);
            }
        }
    }

    protected void BroadcastToAllExcept(IPacket packet, IPEndPoint excludeEndPoint)
    {
        string clientInfo = $"{excludeEndPoint.Address}:{excludeEndPoint.Port}";
        BroadcastToAllExcept(packet, clientInfo);
    }
}