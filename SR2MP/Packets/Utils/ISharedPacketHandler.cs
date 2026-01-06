using System.Net;

namespace SR2MP.Packets.Utils;

public interface ISharedPacketHandler : IClientPacketHandler, IServerPacketHandler
{
    void Handle(byte[] data, IPEndPoint? clientEp = null);
}