using Il2Cpp;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.GordoEat)]
    public class GordoHandler : BaseClientPacketHandler
    {
        public GordoHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<GordoEatPacket>();

            var obj = GameObject.Find(packet.Id);
            if (obj != null)
            {
                var gordo = obj.GetComponent<GordoEat>();
                if (gordo != null && gordo.GordoModel != null)
                {
                    GlobalVariables.handlingPacket = true;
                    gordo.GordoModel.GordoEatenCount = packet.Count;
                    GlobalVariables.handlingPacket = false;
                }
            }
            else
            {
                MelonLoader.MelonLogger.Warning($"GordoHandler: Could not find Gordo with name {packet.Id}");
            }
        }
    }
}
