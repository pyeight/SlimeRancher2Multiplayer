using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.Slime;
using Il2CppMonomiPark.SlimeRancher.VFX;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    private static GadgetModel? GetLinkedGadget(GadgetModel model)
        => GameState.identifiables._entries.FirstOrDefault(x =>
                x.value != null &&
                model != null &&
                x.value.ident == model?.ident
                && model != x.value
                && (model.ident.Cast<GadgetDefinition>().BuyInPairs
                    || model.ident.Cast<GadgetDefinition>().LinkedDefinition
                    || Math.Abs(model.ident.Cast<GadgetDefinition>().LinkedGadgetRange) > 0.0001f))?
            .value.Cast<GadgetModel>()!;
    
    private static AmmoModel? GetAmmoFromGadget(GadgetModel model)
    {
        if (model.TryCast(out WarpDepotModel? warp))
            return warp.ammo;
        
        return null!;
    }
    
    internal void SendActorTypeRegistry(IPEndPoint clientEndPoint)
    {
        if (!Main.Server.IsRunning) return;

        var packet = new ActorTypeRegistryPacket
        {
            Registry = new Dictionary<int, string>(ActorTypes.Count)
        };

        foreach (var (persistentId, type) in ActorTypes)
        {
            if (type == null) continue;
            packet.Registry[persistentId] = type.ReferenceId;
        }

        Main.Server.SendToClient(packet, clientEndPoint);
    }
    
    internal static bool ApplyRadiancy(SlimeModel slime, ActorAppearanceType radiancy = ActorAppearanceType.Default)
    {
        if (slime == null) return false;
        
        var gameObj = slime.GetGameObject();
        if (!gameObj) return false;
        
        var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
        if (!applicator) return false;
        
        var def = gameObj.GetComponent<Identifiable>().identType.TryCast<SlimeDefinition>();
        if (!def) return false;
        
        if (radiancy == ActorAppearanceType.Default && slime.IsRadiant)
        {
            if (def!.RadiantBase && def.RadiantBase.AppearType == SlimeAppearance.AppearanceType.RADIANT_BASE)
                radiancy = ActorAppearanceType.BaseRadiant;
            else if (def.RadiantLargo0 &&
                     def.RadiantLargo0.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_0)
                radiancy = ActorAppearanceType.LargoRadiant0;
            else if (def.RadiantLargo1 &&
                     def.RadiantLargo1.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_1)
                radiancy = ActorAppearanceType.LargoRadiant1;
        }
        
        var newAppearance = radiancy switch
        {
            ActorAppearanceType.BaseRadiant => def!.RadiantBase,
            ActorAppearanceType.LargoRadiant0 => def!.RadiantLargo0,
            ActorAppearanceType.LargoRadiant1 => def!.RadiantLargo1,
            _ => applicator.Appearance
        };
        
        if (!newAppearance) return false;
        
        var slimeRadiant = gameObj.GetComponent<SlimeRadiant>();
        if (slimeRadiant)
        {
            slimeRadiant.SetRadiant();
            slimeRadiant.SetRadiantAppearance();
        }
        
        slime.GetAmmoMetadata().Radiant = true;
        applicator.Appearance = newAppearance;
        applicator.ApplyAppearance();
        applicator.HandleChosenAppearanceChanged(def, newAppearance);
        
        return true;
    }
    
    private static IEnumerator EnsureSlimeAppearance(SlimeModel slime)
    {
        yield return new WaitFrames(2);

        if (slime == null) yield break;

        var gameObj = slime.GetGameObject();
        if (!gameObj) yield break;

        var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
        if (!applicator || applicator.Appearance) yield break;

        if (slime.IsRadiant && ApplyRadiancy(slime)) yield break;

        var def = gameObj.GetComponent<Identifiable>()?.identType.TryCast<SlimeDefinition>();
        if (!def) yield break;

        var appearance = def!.IsLargo
            ? def.GetLargoAppearance(slime.firstAppearanceSaveSet, slime.secondAppearanceSaveSet)
            : def.GetAppearanceForSet(slime.firstAppearanceSaveSet);

        if (!appearance)
            appearance = SceneContext.Instance?.SlimeAppearanceDirector?.GetChosenSlimeAppearance(def);

        if (!appearance)
            appearance = def.GetDefaultAppearance();

        if (!appearance) yield break;

        applicator.Appearance = appearance;
        applicator.ApplyAppearance();
    }

    internal static IEnumerator ApplySprinkleMaterial(GameObject gameObj, SprinkleMaterialType material)
    {
        yield return new WaitFrames(2);
        
        if (!gameObj) yield break;
        
        var sprinkle = gameObj.GetComponent<RandomMaterial>();
        if (!sprinkle) yield break;

        sprinkle.SetMaterial((int)material);
    }

    internal static void ApplyOwnership(ActorTransferPacket packet)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;

        if (!actor.TryGetNetworkComponent(out var component))
            return;
        
        component.CurrentOwnerId = packet.OwnerId;

        var locallyOwned = packet.OwnerId == LocalID;

        if (!locallyOwned)
        {
            try
            {
                var player = SceneContext.Instance.Player;
                var gameObject = actor.GetGameObject();

                if (player && gameObject)
                {
                    var vacItem = player.GetComponent<PlayerItemController>()._vacuumItem;

                    if (vacItem && vacItem._held == gameObject)
                    {
                        vacItem.LockJoint.connectedBody = null;
                        vacItem._held = null;
                        vacItem.SetHeldRad(0f);
                        vacItem._vacMode = VacuumItem.VacMode.NONE;
                        gameObject.GetComponent<Vacuumable>().Release();
                    }
                }
            }
            catch (Exception exception)
            {
                SrLogger.LogDebug($"Failed to release actor from vacuum on ownership change: {exception.Message}");
            }
        }

        component.LocallyOwned = locallyOwned;
    }
    
    internal static void RemoveExistingGadgetModel(ActorId actorId)
    {
        if (actorId.Value == 0) return;

        try
        {
            foreach (var gadget in GameState.AllGadgets())
            {
                if (gadget == null || gadget.actorId.Value != actorId.Value)
                    continue;

                var gameObject = gadget.GetGameObject();

                HandlingPacket = true;
                if (gameObject)
                    Destroyer.DestroyGadget(gameObject, "SR2MP.RemoveExistingGadgetModel");
                else
                    GameState.DestroyGadgetModel(gadget);
                HandlingPacket = false;
                
                var mapDirector = SceneContext.Instance?.MapDirector;
                if (mapDirector != null)
                    mapDirector.DeregisterMarker(gadget);

                break;
            }
        }
        catch (Exception ex)
        {
            HandlingPacket = false;
            SrLogger.LogWarning($"Failed to remove existing gadget model for {actorId.Value}: {ex.Message}");
        }
    }
}