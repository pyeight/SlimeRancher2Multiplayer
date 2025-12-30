using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers
{
    [PacketHandler((byte)PacketType.GordoEat)]
    public class GordoEatHandler : BasePacketHandler
    {
        public GordoEatHandler(NetworkManager nm, ClientManager cm) : base(nm, cm) { }
        public override void Handle(byte[] data, string sender) => Main.Server.SendToAllExcept(data, sender);
    }

    [PacketHandler((byte)PacketType.TreasurePod)]
    public class TreasurePodHandler : BasePacketHandler
    {
        public TreasurePodHandler(NetworkManager nm, ClientManager cm) : base(nm, cm) { }
        public override void Handle(byte[] data, string sender) => Main.Server.SendToAllExcept(data, sender);
    }

    [PacketHandler((byte)PacketType.MapUnlock)]
    public class MapUnlockHandler : BasePacketHandler
    {
        public MapUnlockHandler(NetworkManager nm, ClientManager cm) : base(nm, cm) { }
        public override void Handle(byte[] data, string sender) => Main.Server.SendToAllExcept(data, sender);
    }
}
