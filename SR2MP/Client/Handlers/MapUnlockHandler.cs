using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.UI;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.MapUnlock)]
    public class MapUnlockHandler : BaseClientPacketHandler
    {
        public MapUnlockHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<MapUnlockPacket>();

            var obj = GameObject.Find(packet.Id);
            if (obj != null)
            {
                var tech = obj.GetComponent<TechUIInteractable>();
                if (tech != null)
                {
                    GlobalVariables.handlingPacket = true;
                    tech.OnInteract();
                    GlobalVariables.handlingPacket = false;
                }
            }
            else
            {
                MelonLoader.MelonLogger.Warning($"MapUnlockHandler: Could not find Map Object with name {packet.Id}");
            }
        }
    }
}
