using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using UnityEngine;

namespace SR2MP.Client.Handlers
{
    [PacketHandler((byte)PacketType.BlueprintUnlock)]
    public class BlueprintHandler : BaseClientPacketHandler
    {
        public BlueprintHandler(Client client, RemotePlayerManager playerManager)
            : base(client, playerManager) { }

        public override void Handle(byte[] data)
        {
            using var reader = new PacketReader(data);
            var packet = reader.ReadPacket<BlueprintUnlockPacket>();

            if (SceneContext.Instance.GadgetDirector != null && GameContext.Instance.LookupDirector != null)
            {
                var def = GlobalVariables.actorManager.ActorTypes.Values.FirstOrDefault(t => t.name == packet.Id)?.Cast<GadgetDefinition>();
                if (def != null)
                {
                    GlobalVariables.handlingPacket = true;
                    // Check if already unlocked?
                    if (!SceneContext.Instance.GadgetDirector.HasBlueprint(def))
                    {
                        SceneContext.Instance.GadgetDirector.AddBlueprint(def);
                        MelonLoader.MelonLogger.Msg($"Unlocked Blueprint via MP: {packet.Id}");
                    }
                    GlobalVariables.handlingPacket = false;
                }
                else
                {
                   MelonLoader.MelonLogger.Warning($"BlueprintHandler: Could not find GadgetDef {packet.Id}");
                }
            }
        }
    }
}
