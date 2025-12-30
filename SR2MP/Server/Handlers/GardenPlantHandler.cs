using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers
{
    [PacketHandler((byte)PacketType.GardenPlant)]
    public class GardenPlantHandler : BasePacketHandler
    {
        public GardenPlantHandler(NetworkManager networkManager, ClientManager clientManager) 
            : base(networkManager, clientManager) { }

        public override void Handle(byte[] data, string clientIdentifier)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<GardenPlantPacket>();

            // Broadcast to all other clients
            Main.Server.SendToAllExcept(packet, clientIdentifier);
        }
    }
}
