using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using SR2MP.Components.World;
using UnityEngine;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.Gadget)]
public class GadgetHandler : BaseClientPacketHandler
{
    public GadgetHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<GadgetPacket>();

        GlobalVariables.handlingPacket = true;
        try
        {
            if (packet.IsRemoval)
            {
                if (GlobalVariables.gadgetsById.TryGetValue(packet.GadgetId, out var gadgetObj))
                {
                    if (gadgetObj != null)
                    {
                        Object.Destroy(gadgetObj);
                    }
                    GlobalVariables.gadgetsById.Remove(packet.GadgetId);
                }
            }
            else
            {
                // Convert int ID back to IdentifiableType
                var gadgetDef = GlobalVariables.actorManager.GetIdentifiableType(packet.GadgetTypeId);
                if (gadgetDef != null)
                {
                    var definition = gadgetDef.Cast<GadgetDefinition>();
                    
                    if (definition != null && definition.prefab != null)
                    {
                        // Signal our Gadget.Awake patch to use this ID
                        GlobalVariables.currentlyInstantiatingGadgetId = packet.GadgetId;

                        var sceneGroup = SystemContext.Instance.SceneLoader.DefaultGameplaySceneGroup;
                        GadgetDirector.InstantiateGadget(definition.prefab, sceneGroup, packet.Position, packet.Rotation, true);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            SrLogger.LogError($"Error handling GadgetPacket: {e}");
        }
        finally
        {
            GlobalVariables.handlingPacket = false;
            GlobalVariables.currentlyInstantiatingGadgetId = string.Empty; // Just in case
        }
    }
}
