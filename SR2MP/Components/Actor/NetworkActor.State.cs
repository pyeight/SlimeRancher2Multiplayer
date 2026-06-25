using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using Unity.Mathematics;

namespace SR2MP.Components.Actor;

internal sealed partial class NetworkActor
{
    private float4 lastSentEmotions;
    private bool lastSentSleeping;
    private double lastSentResourceProgress;
    private ResourceCycle.State lastSentResourceState;
    private bool lastSentInvulnerable;
    private float lastSentInvulnerablePeriod;

    private void SendStateUpdate()
    {
        if (!isSlime && !isResource && !isPlort)
            return;

        if (!ShouldUpdateState())
            return;

        var actorId = ActorId;

        if (actorId.Value == 0)
            return;

        Main.SendToAllOrServer(BuildStatePacket(actorId));
    }

    private bool ShouldUpdateState()
    {
        if (isSlime)
        {
            var currentEmotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
            var currentSleeping = emotions && emotions._model.isSleeping;

            return !currentEmotions.Equals(lastSentEmotions) || currentSleeping != lastSentSleeping;
        }

        if (isResource && cycle?._model != null)
            return cycle._model.state != lastSentResourceState;

        if (isPlort)
        {
            plortModel ??= GetComponent<PlortModel>();

            var currentInvulnerable       = plortModel?._invulnerability?.IsInvulnerable       ?? false;
            var currentInvulnerablePeriod = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;

            return currentInvulnerable != lastSentInvulnerable || !Mathf.Approximately(currentInvulnerablePeriod, lastSentInvulnerablePeriod);
        }

        return false;
    }

    private ActorStatePacket BuildStatePacket(ActorId actorId)
    {
        if (isSlime)
        {
            var currentEmotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
            var currentSleeping = emotions && emotions._model.isSleeping;

            lastSentEmotions = currentEmotions;
            lastSentSleeping = currentSleeping;

            return new ActorStatePacket
            {
                ActorId    = actorId,
                UpdateType = ActorUpdateType.Slime,
                Emotions   = currentEmotions,
                Sleeping   = currentSleeping
            };
        }

        if (isResource)
        {
            var currentState    = cycle!._model!.state;
            var currentProgress = cycle._model.progressTime;

            lastSentResourceState    = currentState;
            lastSentResourceProgress = currentProgress;

            return new ActorStatePacket
            {
                ActorId          = actorId,
                UpdateType       = ActorUpdateType.Resource,
                ResourceState    = currentState,
                ResourceProgress = currentProgress
            };
        }
        
        plortModel ??= GetComponent<PlortModel>();

        var invulnerable       = plortModel?._invulnerability?.IsInvulnerable        ?? false;
        var invulnerablePeriod = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;

        lastSentInvulnerable       = invulnerable;
        lastSentInvulnerablePeriod = invulnerablePeriod;

        return new ActorStatePacket
        {
            ActorId            = actorId,
            UpdateType         = ActorUpdateType.Plort,
            Invulnerable       = invulnerable,
            InvulnerablePeriod = invulnerablePeriod
        };
    }
}