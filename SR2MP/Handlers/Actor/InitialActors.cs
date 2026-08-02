using System.Collections;
using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.InitialActors, HandlerType.Client)]
internal sealed class ActorsLoadHandler : BasePacketHandler<InitialActorsPacket>
{
    protected override bool Handle(InitialActorsPacket packet, IPEndPoint? _)
    {
        StartCoroutine(SpawnWhenReady(packet));

        return false;
    }

    private static bool IsSaveLoaded()
    {
        try
        {
            return SceneContext.Instance != null
                   && SceneContext.Instance.player
                   && !SystemContext.Instance.SceneLoader.IsSceneLoadInProgress;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerator SpawnWhenReady(InitialActorsPacket packet)
    {
        var waited = 0f;
        while (!IsSaveLoaded())
        {
            if (!Main.Client.IsConnected && !Main.Server.IsRunning)
                yield break;

            if (waited > 180f)
            {
                SrLogger.LogWarning("InitialActors: world never loaded, dropping initial actor packet.");
                yield break;
            }

            waited += UnityEngine.Time.unscaledDeltaTime;
            yield return null;
        }

        var destroyedSomething = false;
        try
        {
            destroyedSomething = DestroyExistingActors();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"InitialActors: destroy phase failed: {ex.Message}");
        }
        
        // We need to wait for the Teleporters to correctly unlink their old stuff
        if (destroyedSomething)
        {
            yield return null;
            yield return null;
        }

        try
        {
            SpawnInitialActors(packet);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"InitialActors: spawn phase failed: {ex.Message}");
        }
    }

    private static bool DestroyExistingActors()
    {
        ActorManager.Actors.Clear();

        NetworkGadgetManager.CacheTeleporterStates();

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            GameState.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        var destroyed = false;

        HandlingPacket = true;
        try
        {
            foreach (var (id, value) in toRemove)
            {
                if (value.ident.IsPlayer)
                    continue;

                if (value.ident.IsGadget())
                {
                    NetworkActorManager.RemoveExistingGadgetModel(id);
                    destroyed = true;
                }

                var gameObject = value.GetGameObject();

                if (gameObject)
                {
                    Destroyer.DestroyAny(gameObject, "SR2MP.InitialActors");
                    destroyed = true;
                }
                else if (!value.ident.IsGadget())
                {
                    // Hibernation caused problems, no gameObj
                    GameState.DestroyIdentifiableModel(value);
                    destroyed = true;
                }
            }
        }
        finally
        {
            HandlingPacket = false;
        }

        return destroyed;
    }

    private static void SpawnInitialActors(InitialActorsPacket packet)
    {
        GameState._actorIdProvider._nextActorId = packet.StartingActorID;
        GameState.world.worldTime = packet.WorldTime;

        foreach (var actor in packet.Actors)
        {
            try
            {
                ActorManager.TrySpawnInitialActor(actor, out var _);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to spawn initial actor {actor.ActorId} (type_{actor.ActorTypeId}): {ex.Message}");
            }
        }

        ActorManager.TakeOwnershipOfNearby();
        
        StartCoroutine(NetworkGadgetManager.RelinkGadgets());
        StartCoroutine((NetworkGadgetManager.RepairTeleporterLinks()));
    }
}