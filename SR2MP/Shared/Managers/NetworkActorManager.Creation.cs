using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Drone;
using Il2CppMonomiPark.SlimeRancher.VFX;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public static InitialActorsPacket.ActorBase CreateInitialActor(IdentifiableModel actor)
    {
        if (actor.TryCast<SlimeModel>(out var slime))
            return CreateInitialSlime(slime);

        if (actor.TryCast<PlortModel>(out var plort))
            return CreateInitialPlort(plort);

        if (actor.TryCast<ProduceModel>(out var resource))
            return CreateInitialResource(resource);

        var gameObj = actor.GetGameObject();
        if (gameObj && gameObj.GetComponent<RandomMaterial>() != null)
            return CreateInitialSprinkle(actor);

        return CreateInitialActorBase(actor);
    }

    private static InitialActorsPacket.ActorBase CreateInitialActorBase(IdentifiableModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.TryCast<ActorModel>()?.lastRotation ?? Quaternion.identity,
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };

    private static InitialActorsPacket.Slime CreateInitialSlime(SlimeModel model)
    {
        var gameObj = model.GetGameObject();
        var radiancy = ActorAppearanceType.Default;

        if (gameObj)
        {
            var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
            var def = gameObj.GetComponent<Identifiable>()?.identType.TryCast<SlimeDefinition>();
            if (applicator && def)
            {
                var current = applicator.Appearance;
                if (current == def!.RadiantBase)
                    radiancy = ActorAppearanceType.BaseRadiant;
                else if (current == def.RadiantLargo0)
                    radiancy = ActorAppearanceType.LargoRadiant0;
                else if (current == def.RadiantLargo1)
                    radiancy = ActorAppearanceType.LargoRadiant1;
            }
        }

        return new InitialActorsPacket.Slime
        {
            ActorId = model.actorId.Value,
            ActorTypeId = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = model.lastRotation,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            Emotions = model.Emotions,
            Sleeping = model.isSleeping,
            Radiancy = (int)radiancy
        };
    }

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

    private static InitialActorsPacket.Sprinkle CreateInitialSprinkle(IdentifiableModel model)
    {
        byte materialIndex = 0;
        var gameObj = model.GetGameObject();
        if (gameObj)
        {
            var sprinkle = gameObj.GetComponent<RandomMaterial>();
            if (sprinkle)
            {
                var currentMaterial = sprinkle._renderers?[0]?.sharedMaterial;
                if (currentMaterial != null)
                {
                    var materialName = currentMaterial.name.Replace(" (Instance)", "");
                    if (Enum.TryParse(materialName, out SprinkleMaterialType type))
                        materialIndex = (byte)type;
                }
            }
        }

        return new InitialActorsPacket.Sprinkle
        {
            ActorId = model.actorId.Value,
            ActorTypeId = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = model.TryCast<ActorModel>()?.lastRotation ?? Quaternion.identity,
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            MaterialIndex = materialIndex
        };
    }

    public static InitialActorsPacket.ActorBase CreateInitialGadget(GadgetModel gadget)
    {
        if (gadget.TryCast<DroneStationGadgetModel>(out _))
            return CreateInitialDroneStation(gadget.Cast<DroneStationGadgetModel>());

        if (GetLinkedGadget(gadget) != null)
        {
            if (GetAmmoFromGadget(gadget) != null)
                return CreateInitialAmmoGadget(gadget);

            return CreateInitialLinkedGadget(gadget);
        }

        return CreateInitialGadgetBase(gadget);
    }

    private static InitialActorsPacket.ActorBase CreateInitialGadgetBase(GadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup)
    };

    private static InitialActorsPacket.LinkedGadget CreateInitialLinkedGadget(GadgetModel model) => new()
    {
        ActorId = model.actorId.Value,
        ActorTypeId = GetPersistentID(model.ident),
        Position = model.lastPosition,
        Rotation = model.GetRot(),
        Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
        LinkedActorId = GetLinkedGadget(model)!.actorId.Value
    };

    private static InitialActorsPacket.LinkedAmmoGadget CreateInitialAmmoGadget(GadgetModel model)
    {
        var ammoSlots = new Dictionary<int, NetworkAmmoSlot>();
        var ammoModel = GetAmmoFromGadget(model);
        if (ammoModel != null && ammoModel.Slots != null)
        {
            var slots = ammoModel.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;

                if (slot.Definition == null) continue;

                ammoSlots[i] = new NetworkAmmoSlot
                {
                    Count = slot.Count,
                    MaxCount = slot.MaxCount,
                    Identifiable = GetPersistentID(slot._id),
                    SlotDefinition = NetworkAmmoManager.GetId(slot.Definition)
                };
            }
        }

        return new InitialActorsPacket.LinkedAmmoGadget
        {
            ActorId = model.actorId.Value,
            ActorTypeId = GetPersistentID(model.ident),
            Position = model.lastPosition,
            Rotation = model.GetRot(),
            Scene = NetworkSceneManager.GetPersistentID(model.sceneGroup),
            LinkedActorId = GetLinkedGadget(model)!.actorId.Value,
            Ammo = new NetworkAmmo { AmmoSlots = ammoSlots }
        };
    }

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
        Charge = model.GetCurrEnergy(SceneContext.Instance.TimeDirector, 0.8333f),
        Task = new InitialActorsPacket.DroneTask
        {
            TargetIdent = GetPersistentID(model._taskData.TargetIdentType),
            Sink = model._taskData.SinkType,
            Target = model._taskData.TargetType,
            Source = model._taskData.SourceType,
        },
        LinkedActorId = model._type == DroneType.EXPLORER_DRONE
            ? GameState.droneModel.GetExplorerDrone(model.actorId).actorId.Value
            : GameState.droneModel.GetRanchDrone(model.actorId).actorId.Value
    };
}