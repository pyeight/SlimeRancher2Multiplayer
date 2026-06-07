using SR2MP.Packets.Actor;

namespace SR2MP.Components.Actor;

internal sealed partial class NetworkActor
{
    private ResourceCycle.State? prevResourceState;

    private void UpdateResourceState()
    {
        if (!isResource || LocallyOwned || cycle == null || cycle._model == null || ShouldUpdateResourceState)
            return;

        cycle._model.progressTime = double.MaxValue;
        ShouldUpdateResourceState = false;
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

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        if (cycle == null)
            return;

        ShouldUpdateResourceState = true;

        if (cycle._model != null)
            cycle._model.progressTime = progress;

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