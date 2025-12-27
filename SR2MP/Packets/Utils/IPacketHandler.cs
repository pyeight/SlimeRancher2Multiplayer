using System.Net;

namespace SR2MP.Packets.Utils;

public interface IPacketHandler
{
    void Handle(byte[] data, IPEndPoint clientEp);
}