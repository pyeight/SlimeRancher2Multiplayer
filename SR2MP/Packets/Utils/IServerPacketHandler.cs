using System.Net;

namespace SR2MP.Packets.Utils;

public interface IServerPacketHandler : IPacketHandler
{
    void Handle(byte[] data, IPEndPoint? clientEp);
}