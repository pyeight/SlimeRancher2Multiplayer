using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public sealed partial class NetworkActorManager
{
    public static InitialActorsPacket.ActorBase CreateInitialActor(IdentifiableModel actor)
    {
        if (actor.TryCast<SlimeModel>(out var slime))
            return CreateInitialSlime(slime);

        if (actor.TryCast<PlortModel>(out var plort))
            return CreateInitialPlort(plort);

        if (actor.TryCast<ProduceModel>(out var resource))
            return CreateInitialResource(resource);

        return CreateInitialActorBase(actor);
    }

    public static InitialActorsPacket.ActorBase CreateInitialGadget(GadgetModel gadget)
    {
        if (gadget.TryCast<DroneStationGadgetModel>(out var drone))
            
        
        if (GetLinkedGadget(gadget) != null)
        {
            if (GetAmmoFromGadget(gadget) != null)
                return CreateInitialAmmoGadget(gadget);
            
            return CreateInitialLinkedGadget(gadget);
        }
        return CreateInitialGadgetBase(gadget);
    }

    private static InitialActorsPacket.ActorBase CreateInitialActorBase(IdentifiableModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.TryCast<ActorModel>()?.lastRotation ?? Quaternion.identity,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };
    private static InitialActorsPacket.ActorBase CreateInitialGadgetBase(GadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };
    private static InitialActorsPacket.DroneStation CreateInitialDroneStation(DroneStationGadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DroneType = model._type,
        DroneInStation = model._isDroneAtStation,
        // 0.8333 is looking quite random, but i couldnt find an actual const or variable that gave the correct output.
        // This number was taken from a UE Hook on GetCurrEnergy on DroneStationGadgetModel.
        Charge = model.GetCurrEnergy(SceneContext.Instance.TimeDirector, 0.8333f)  
    };

    private static InitialActorsPacket.Slime CreateInitialSlime(SlimeModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        Emotions = model.Emotions
    };
    private static InitialActorsPacket.LinkedGadget CreateInitialLinkedGadget(GadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        LinkedActorId = GetLinkedGadget(model).actorId.Value
    };
    private static InitialActorsPacket.LinkedAmmoGadget CreateInitialAmmoGadget(GadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        LinkedActorId = GetLinkedGadget(model)!.actorId.Value,
        Ammo = new NetworkAmmo()
        {
            AmmoSlots = GetAmmoFromGadget(model)!.Slots
                .ToDictionary<AmmoSlot, int, NetworkAmmoSlot>(
                    slot => (int)slot.GetSlotIndex()!,
                    slot => new NetworkAmmoSlot()
                    {
                        Count = slot.Count,
                        Identifiable = GetPersistentID(slot._id),
                        SlotDefinition = NetworkAmmoManager.GetId(slot.Definition)
                    })
        }
    };

    private static InitialActorsPacket.Plort CreateInitialPlort(PlortModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.lastRotation,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        DestroyTime = model.destroyTime,
        Invulnerable = model._invulnerability?.IsInvulnerable ?? false,
        InvulnerablePeriod = model._invulnerability?.InvulnerabilityPeriod ?? 0f
    };

    private static InitialActorsPacket.Resource CreateInitialResource(ProduceModel model)
    {
        var packet = new InitialActorsPacket.Resource
        {
            ActorId = model.actorId.Value,
            ActorTypeId = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = model.lastRotation,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            DestroyTime = model.destroyTime,
            ResourceState = model._state,
            ProgressTime = model.progressTime,
            JointIndex = -1,
            PlotID = string.Empty,
            SpawnerPosition = Vector3.zero
        };
        
        var obj = model.GetGameObject();
        if (!obj) return packet;

        var cycle = obj.GetComponent<ResourceCycle>();
        if (!cycle || cycle._joint == null) return packet;
        
        var joint = cycle._joint.Joint;
        if (!joint) return packet;

        var spawner = joint.gameObject.GetComponentInParent<SpawnResource>();
        if (!spawner) return packet;
        
        packet.JointIndex = spawner.SpawnJoints.IndexOf(joint);
        packet.SpawnerPosition = spawner.transform.position;
        packet.PlotID = joint.gameObject.GetComponentInParent<LandPlotLocation>()?._id ?? string.Empty;

        return packet;
    }

    public static ActorUpdateType DetermineUpdateTypeFromModel(ActorModel model)
    {
        if (model.TryCast<SlimeModel>() != null)
            return ActorUpdateType.Slime;
        if (model.TryCast<ProduceModel>() != null)
            return ActorUpdateType.Resource;
        if (model.TryCast<PlortModel>() != null)
            return ActorUpdateType.Plort;
        
        return ActorUpdateType.Actor;
    }
    // not sure if there are more...
    private static AmmoModel? GetAmmoFromGadget(GadgetModel model)
    {
        if (model.TryCast(out WarpDepotModel? warp))
            return warp.ammo;
        
        return null!;
    }
}