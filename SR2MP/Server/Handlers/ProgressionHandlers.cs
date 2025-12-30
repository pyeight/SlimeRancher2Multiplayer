using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers
{
    [PacketHandler((byte)PacketType.MarketUpdate)]
    public class MarketUpdateHandler : BasePacketHandler
    {
        public MarketUpdateHandler(NetworkManager nm, ClientManager cm) : base(nm, cm) { }
        public override void Handle(byte[] data, string sender) => Main.Server.SendToAllExcept(data, sender);
    }

    [PacketHandler((byte)PacketType.BlueprintUnlock)]
    public class BlueprintUnlockHandler : BasePacketHandler
    {
        public BlueprintUnlockHandler(NetworkManager nm, ClientManager cm) : base(nm, cm) { }
        public override void Handle(byte[] data, string sender) => Main.Server.SendToAllExcept(data, sender);
    }
}
