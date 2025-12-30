using System.Linq;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.MarketUpdate)]
    public class MarketHandler : BaseClientPacketHandler
    {
        public MarketHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<MarketUpdatePacket>();

            // Apply prices
            /*
            if (SceneContext.Instance.EconomyDirector != null  && GlobalVariables.actorManager != null)
            {
                GlobalVariables.handlingPacket = true;
                
                var economy = SceneContext.Instance.EconomyDirector;
                
                // Helper to find type by name
                foreach (var kvp in packet.Prices)
                {
                    var type = GlobalVariables.actorManager.ActorTypes.Values.FirstOrDefault(t => t.name == kvp.Key);
                    if (type != null)
                    {
                    }
                }
                
                GlobalVariables.handlingPacket = false;
            }
            */
        }
    }
}
