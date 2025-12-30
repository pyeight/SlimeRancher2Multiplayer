using Il2Cpp;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.TreasurePod)]
    public class TreasurePodHandler : BaseClientPacketHandler
    {
        public TreasurePodHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<TreasurePodPacket>();

            var obj = GameObject.Find(packet.Id);
            if (obj != null)
            {
                var pod = obj.GetComponent<TreasurePod>();
                if (pod != null)
                {
                    GlobalVariables.handlingPacket = true;
                    // Activate typically handles checks, but we want to force open?
                    // Or sync state? Activate usually opens it.
                    pod.Activate(); 
                    GlobalVariables.handlingPacket = false;
                }
            }
            else
            {
                MelonLoader.MelonLogger.Warning($"TreasurePodHandler: Could not find Pod with name {packet.Id}");
            }
        }
    }
}
