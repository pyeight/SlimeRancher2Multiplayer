using LiteNetLib;
using System.Net;

namespace SR2MP.Server.Models;

public sealed class ClientInfo
{
    public NetPeer Peer { get; }
    public string PlayerId { get; set; }

    public ClientInfo(NetPeer peer, string playerId = "")
    {
        Peer = peer;
        PlayerId = playerId;
    }

    public IPEndPoint EndPoint => new IPEndPoint(Peer.Address, Peer.Port);

    public string GetClientInfo() => $"{Peer.Address}:{Peer.Port}";
}
