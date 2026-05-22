using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.LandPlots;

// todo: review
// not sure about the whole coroutine and inactive stuff

[PacketHandler((byte)PacketType.InitialLandPlots, HandlerType.Client)]
internal sealed class InitialLandPlotsHandler : BasePacketHandler<InitialLandPlotsPacket>
{
    protected override bool Handle(InitialLandPlotsPacket packet, IPEndPoint? _)
    {
        foreach (var plot in packet.LandPlots)
        {
            var model = GameState.landPlots[plot.ID];

            if (model.gameObj)
            {
                HandlingPacket = true;
                var location = model.gameObj.GetComponent<LandPlotLocation>();
                var landPlotComponent = model.gameObj.GetComponentInChildren<LandPlot>(true);
                location.Replace(landPlotComponent, GameContext.Instance.LookupDirector._plotPrefabDict[plot.Type]);

                var landPlotComponent2 = model.gameObj.GetComponentInChildren<LandPlot>(true);
                landPlotComponent2.ApplyUpgrades(plot.Upgrades.Cast<CppCollections.IEnumerable<LandPlot.Upgrade>>(), false);
                HandlingPacket = false;
            }

            model.typeId = plot.Type;
            model.upgrades = plot.Upgrades;

            switch (plot.Data)
            {
                case InitialLandPlotsPacket.GardenData { Crop: 9 }:
                {
                    model.resourceGrowerDefinition = null;
                    if (!model.gameObj)
                        continue;
                    var gardenPlot = model.gameObj.GetComponentInChildren<LandPlot>(true);
                    HandlingPacket = true;
                    gardenPlot.DestroyAttached();
                    HandlingPacket = false;
                    break;
                }

                case InitialLandPlotsPacket.GardenData garden:
                {
                    var actor = ActorManager.ActorTypes[garden.Crop];
                    model.resourceGrowerDefinition =
                        GameContext.Instance.AutoSaveDirector._saveReferenceTranslation._resourceGrowerTranslation
                            .RawLookupDictionary._entries.FirstOrDefault(x =>
                                x.value._primaryResourceType == actor)!.value;

                    if (!model.gameObj)
                        continue;

                    var gardenCatcher = model.gameObj.GetComponentInChildren<GardenCatcher>();

                    HandlingPacket = true;
                    if (gardenCatcher.CanAccept(actor))
                    {
                        var plantedObject = gardenCatcher.Plant(actor, true);

                        if (plantedObject != null)
                        {
                            var spawnResource = plantedObject.GetComponent<SpawnResource>();
                            if (spawnResource != null && spawnResource._model != null)
                            {
                                foreach (var joint in spawnResource.SpawnJoints)
                                {
                                    if (joint == null || joint.connectedBody == null)
                                        continue;

                                    var connectedObj = joint.connectedBody.gameObject;
                                    Destroyer.Destroy(connectedObj, "InitialLandPlotsHandler.OnDestroy");
                                }

                                spawnResource._model.nextSpawnTime = double.MaxValue;
                            }
                        }
                    }
                    HandlingPacket = false;
                    break;
                }

                case InitialLandPlotsPacket.SiloData silo:

                    var ammo = silo.Ammo.ToGameAmmo();
                    model.siloAmmo[SiloAmmo] = ammo._ammoModel;
                    model.siloStorageIndices = Array.ConvertAll(silo.SelectedSlots.ToArray(), input => (int)input);

                    if (!model.gameObj) break;

                    var storage = model.gameObj.GetComponentInChildren<SiloStorage>();
                    storage.Ammo = ammo;
                    storage.SetModel(model);

                    foreach (var activator in model.gameObj.GetComponentsInChildren<SiloStorageActivator>())
                        activator.OnActiveSlotChanged();

                    break;

                case InitialLandPlotsPacket.CoopPondData pond:
                    var ammoType = plot.Type == LandPlot.Id.COOP
                        ? FeederAmmo
                        : PlortCollectorAmmo;
                    var collectorAmmo = pond.CollectorAmmo!.ToGameAmmo();
                    model.siloAmmo[ammoType] = collectorAmmo._ammoModel;

                    if (!model.gameObj) break;

                    var pondStorage = model.gameObj.GetComponentInChildren<SiloStorage>(true);

                    if (pondStorage)
                    {
                        pondStorage.Ammo = collectorAmmo;
                        pondStorage.SetModel(model);
                    }
                    break;

                case InitialLandPlotsPacket.CorralData corral:

                    var plortCollectorAmmo = corral.PlortCollectorAmmo.ToGameAmmo();
                    model.siloAmmo[PlortCollectorAmmo] = plortCollectorAmmo._ammoModel;

                    var feederAmmo = corral.AutoFeederAmmo.ToGameAmmo();
                    model.siloAmmo[FeederAmmo] = feederAmmo._ammoModel;

                    model.feederCycleSpeed = (SlimeFeeder.FeedSpeed)corral.AutoFeederSpeed;

                    if (!model.gameObj) break;

                    foreach (var corralStorage in model.gameObj.GetComponentsInChildren<SiloStorage>(true))
                    {
                        switch (corralStorage.AmmoSetReference.Guid)
                        {
                            case PlortCollectorAmmo:
                                corralStorage.Ammo = plortCollectorAmmo;
                                break;

                            case FeederAmmo:
                                corralStorage.Ammo = feederAmmo;
                                break;
                        }
                        corralStorage.SetModel(model);
                    }

                    var feeder = model.gameObj.GetComponentInChildren<FeederUpgrader>(true)
                        .Feeder
                        .transform
                        .GetChild(0)
                        .GetComponent<SlimeFeeder>();

                    if (!feeder)
                    {
                        SrLogger.LogError($"Feeder for {plot.ID} is null, tried to set it to {(SlimeFeeder.FeedSpeed)corral.AutoFeederSpeed}");
                        break;
                    }

                    HandlingPacket = true;
                    feeder?.SetFeederSpeed((SlimeFeeder.FeedSpeed)corral.AutoFeederSpeed);
                    feeder?.SetFeederSpeedIcon((SlimeFeeder.FeedSpeed)corral.AutoFeederSpeed);
                    HandlingPacket = false;

                    break;

                case InitialLandPlotsPacket.IncineratorData incinerator:
                    var incineratorAmmo = incinerator.PlortCollectorAmmo!.ToGameAmmo();
                    model.siloAmmo[PlortCollectorAmmo] = incineratorAmmo._ammoModel;

                    model.ashUnits = incinerator.AshLevel;

                    if (!model.gameObj) break;

                    var incineratorStorage = model.gameObj.GetComponentInChildren<SiloStorage>(true);

                    if (incineratorStorage)
                    {
                        incineratorStorage.Ammo = incineratorAmmo;
                        incineratorStorage.SetModel(model);
                    }

                    model.gameObj.GetComponentInChildren<FillableAshSource>().SetModel(model);
                    break;
            }
        }

        return false;
    }
}