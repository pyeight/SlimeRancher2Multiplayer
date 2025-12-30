using System;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Utils;
using SR2MP.Packets.Shared;
using SR2MP.Shared.Managers;
using SR2MP.Components.World;
using UnityEngine;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialGadgets)]
public class GadgetsLoadHandler : BaseClientPacketHandler
{
    public GadgetsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<InitialGadgetsPacket>();

        SrLogger.LogMessage($"Received {packet.Gadgets.Count} initial gadgets");

        foreach (var gadgetPacket in packet.Gadgets)
        {
            GlobalVariables.handlingPacket = true;
            try
            {
                // Convert int ID back to IdentifiableType
                var gadgetDef = GlobalVariables.actorManager.GetIdentifiableType(gadgetPacket.GadgetTypeId);
                if (gadgetDef != null)
                {
                    var definition = gadgetDef.Cast<GadgetDefinition>();
                    
                    if (definition != null && definition.prefab != null)
                    {
                        // Signal our Gadget.Awake patch to use this ID
                        GlobalVariables.currentlyInstantiatingGadgetId = gadgetPacket.GadgetId;

                        var sceneGroup = SystemContext.Instance.SceneLoader.DefaultGameplaySceneGroup;
                        GadgetDirector.InstantiateGadget(definition.prefab, sceneGroup, gadgetPacket.Position, gadgetPacket.Rotation, true);
                    }
                }
            }
            catch (System.Exception e)
            {
                SrLogger.LogError($"Error loading initial gadget: {e}");
            }
            finally
            {
                GlobalVariables.handlingPacket = false;
                GlobalVariables.currentlyInstantiatingGadgetId = string.Empty;
            }
        }
    }
}
