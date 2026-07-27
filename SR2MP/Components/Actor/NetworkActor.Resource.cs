using SR2MP.Packets.Actor;

namespace SR2MP.Components.Actor;

internal sealed partial class NetworkActor
{
    private ResourceCycle.State? prevResourceState;

    private bool hasKnownResourceState;
    private double knownProgressTime;
    private ResourceCycle.State knownResourceState;
    
    // Non-owners should not cache double.MaxValue as progress time,
    // causes problems on gaining ownership if the cache does not update
    internal static bool IsResourceFrozen(double progress) => progress > 1e300 || double.IsInfinity(progress);

    internal bool IsRotten => isResource && cycle?._model != null && cycle._model.state == ResourceCycle.State.ROTTEN;

    private void UpdateResourceState()
    {
        if (!isResource || LocallyOwned || cycle == null || cycle._model == null)
            return;

        if (cycle._model.state == ResourceCycle.State.ROTTEN)
        {
            DespawnRottenResource();
            return;
        }

        if (ShouldUpdateResourceState)
        {
            ShouldUpdateResourceState = false;
        }
        else
        {
            // Cache the real progress before freezing
            var current = cycle._model.progressTime;
            if (!IsResourceFrozen(current))
            {
                knownProgressTime = current;
                knownResourceState = cycle._model.state;
                hasKnownResourceState = true;
            }

            cycle._model.progressTime = double.MaxValue;
        }
    }

    private void DespawnRottenResource()
    {
        if (IsDestroyed)
            return;

        try
        {
            var actorId = ActorId;
            if (actorId.Value != 0)
                ActorManager.Actors.Remove(actorId.Value);

            IsDestroyed = true;
            Destroyer.DestroyAny(gameObject, "SR2MP.RottenCleanup");
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"KillRottenResource error: {ex.Message}");
        }
    }

    private void RestoreStateOnOwnership()
    {
        if (!isResource || cycle?._model == null)
            return;

        if (hasKnownResourceState && !IsResourceFrozen(knownProgressTime))
        {
            SrLogger.LogGarden($"Resource {ActorId.Value}: resuming ripening on ownership (state={knownResourceState}, progressTime={knownProgressTime})");
            SetResourceState(knownResourceState, knownProgressTime, force: true);
            return;
        }
        
        // Invalid cache, reset it to the current time
        if (IsResourceFrozen(cycle._model.progressTime))
        {
            var now = SceneContext.Instance.TimeDirector.WorldTime();
            if (GardenLogging)
                SrLogger.LogWarning($"Resource {ActorId.Value}: no cached progress on ownership, resetting time (worldTime={now})");
            cycle._model.progressTime = now;
        }
    }

    private void HandleCycleReleasing()
    {
        if (CycleReleasing != cachedCycleReleasing)
        {
            if (CycleReleasing == true)
            {
                var actorId = ActorId;

                if (actorId.Value != 0)
                    Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
            }
        }

        cachedCycleReleasing = CycleReleasing;
    }

    private float GetResourceScaleRatio()
    {
        if (cycle == null || cycle._defaultScale.x <= 0f)
            return 1f;

        return transform.localScale.x / cycle._defaultScale.x;
    }

    internal void ApplyResourceScale(float ratio)
    {
        if (cycle == null || ratio <= 0f || cycle._defaultScale.x <= 0f)
            return;

        transform.localScale = cycle._defaultScale * ratio;
    }

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        if (cycle == null)
            return;

        ShouldUpdateResourceState = true;

        knownResourceState = state;
        
        // Do not cache the frozen state
        if (!IsResourceFrozen(progress))
        {
            knownProgressTime = progress;
            hasKnownResourceState = true;

            if (cycle._model != null)
                cycle._model.progressTime = progress;
        }

        if (!force && prevResourceState == state)
            return;

        prevResourceState = state;

        try
        {
            if (cycle._model != null)
                cycle._model.state = state;

            ApplyResourceStateChanges(state);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"SetResourceState error: {ex}");
        }
    }

    private void ApplyResourceStateChanges(ResourceCycle.State state)
    {
        switch (state)
        {
            case ResourceCycle.State.UNRIPE:
                HandleUnripeState();
                break;
            case ResourceCycle.State.RIPE:
                HandleRipeState();
                break;
            case ResourceCycle.State.EDIBLE:
                HandleEdibleState();
                break;
            case ResourceCycle.State.ROTTEN:
                cycle!.SetRotten(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void HandleUnripeState()
    {
        if (gameObject.transform.localScale.x < cycle!._defaultScale.x * 0.33f)
            gameObject.transform.localScale = cycle._defaultScale * 0.33f;

        if (cycle._vacuumable)
            cycle._vacuumable.enabled = false;

        if (rigidbody && cycle._joint != null)
            rigidbody.isKinematic = true;
    }

    private void HandleRipeState()
    {
        if (cycle!._vacuumable)
            cycle._vacuumable.enabled = true;

        if (gameObject.transform.localScale.x < cycle._defaultScale.x)
            gameObject.transform.localScale = cycle._defaultScale;

        if (cycle._joint == null)
            return;

        if (rigidbody)
        {
            rigidbody.isKinematic = false;
            rigidbody.WakeUp();
        }

        cycle.DetachFromJoint();
    }

    private void HandleEdibleState()
    {
        if (cycle!._vacuumable)
        {
            cycle._vacuumable.enabled = true;
            cycle._vacuumable.Pending = false;
        }

        if (rigidbody)
        {
            rigidbody.isKinematic = false;
            rigidbody.WakeUp();
        }

        if (cycle._joint != null)
            cycle.DetachFromJoint();

        cycle._preparingToRelease = false;

        if (cycle.ToShake)
            cycle.ToShake.localPosition = cycle._toShakeDefaultPos;
    }
}