using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using MelonLoader;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;
using Unity.Mathematics;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;

namespace SR2MP.Components.Actor;

[RegisterTypeInIl2Cpp(false)]
internal sealed class NetworkActor : MonoBehaviour
{
    public RegionMember? regionMember;

    private Identifiable identifiable;
    private ResourceCycle? cycle;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;
    private PlortModel plortModel;

    public float syncTimer = Timers.ActorTimer;
    public bool shouldUpdateResourceState;
    public bool isValid = true;
    public bool isDestroyed;
    public byte attemptedGetIdentifiable;
    public bool cachedLocallyOwned;

    private bool? CycleReleasing => cycle?._preparingToRelease;
    private bool? cachedCycleReleasing;
    private ResourceCycle.State? prevResourceState;

    private Vector3 savedVelocity;

    public Vector3 previousPosition;
    public Vector3 nextPosition;
    public Quaternion previousRotation;
    public Quaternion nextRotation;
    public float interpolationStart;
    public float interpolationEnd;

    private bool isSlime;
    private bool isResource;
    private bool isPlort;

    public ActorId ActorId
    {
        get
        {
            if (isDestroyed)
            {
                isValid = false;
                return new ActorId(0);
            }

            if (identifiable != null)
                return GetActorIdSafe();

            if (attemptedGetIdentifiable >= 10)
            {
                SrLogger.LogWarning("Failed to get Identifiable after 10 attempts");
                isValid = false;
                return new ActorId(0);
            }

            try
            {
                identifiable = GetComponent<Identifiable>();
                attemptedGetIdentifiable++;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to get Identifiable component: {ex.Message}");
                attemptedGetIdentifiable++;
                isValid = false;
                return new ActorId(0);
            }

            return identifiable ? GetActorIdSafe() : new ActorId(0);
        }
    }

    private ActorId GetActorIdSafe()
    {
        try
        {
            return identifiable.GetActorId();
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to get ActorId: {ex.Message}");
            isValid = false;
            return new ActorId(0);
        }
    }

    public bool LocallyOwned { get; set; }

    private void Start()
    {
        try
        {
            if (GetComponent<Gadget>() || GetComponent<SRCharacterController>())
            {
                Destroy(this);
                return;
            }

            emotions = GetComponent<SlimeEmotions>();
            rigidbody = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();
            cycle = GetComponent<ResourceCycle>();
            regionMember = GetComponent<RegionMember>();
            cachedLocallyOwned = LocallyOwned;

            GetActorType();

            if (regionMember != null)
                SetupHibernationEvent();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Start error: {ex}");
            isValid = false;
        }
    }

    private void GetActorType()
    {
        if (ActorId.Value == 0 || !GameState.identifiables.TryGetValue(ActorId, out var identModel))
            return;

        isSlime = identModel.TryCast<SlimeModel>() != null;
        isResource = identModel.TryCast<ProduceModel>() != null;
        isPlort = identModel.TryCast<PlortModel>() != null;
    }

    private void SetupHibernationEvent()
    {
        try
        {
            var delegateType = Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                ?.GetEvent("BeforeHibernationChanged")
                ?.EventHandlerType;

            if (delegateType == null)
                return;

            var hibernationDelegate = Delegate.CreateDelegate(
                delegateType,
                Cast<Il2CppSystem.Object>(),
                nameof(HibernationChanged),
                true);

            regionMember?.add_BeforeHibernationChanged(hibernationDelegate.Cast<RegionMember.OnHibernationChange>());
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to add hibernation event: {ex.Message}");
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator WaitOneFrameOnHibernationChange(bool hibernating)
    {
        yield return null;

        if (!isValid || isDestroyed)
            yield break;

        try
        {
            var actorId = ActorId;

            if (actorId.Value == 0)
                yield break;

            LocallyOwned = !hibernating;

            if (hibernating)
                Main.SendToAllOrServer(new ActorUnloadPacket { ActorId = actorId });
            else
                Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"WaitOneFrameOnHibernationChange error: {ex}");
            isValid = false;
        }
    }

    public void HibernationChanged(bool value)
    {
        if (!isValid || isDestroyed)
            return;

        try
        {
            MelonCoroutines.Start(WaitOneFrameOnHibernationChange(value));
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"HibernationChanged error: {ex}");
        }
    }

    public void OnNetworkUpdate(ActorUpdatePacket packet)
    {
        if (LocallyOwned || isDestroyed)
            return;

        previousPosition = transform.position;
        previousRotation = transform.rotation;
        nextPosition = packet.Position;
        nextRotation = packet.Rotation;
        savedVelocity = packet.Velocity;
        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd = interpolationStart + Timers.ActorTimer;
    }

    private void UpdateInterpolation()
    {
        if (LocallyOwned || isDestroyed || interpolationEnd <= interpolationStart)
            return;

        var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        timer = Mathf.Clamp01(timer);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, timer);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, timer);

        if (rigidbody)
            rigidbody.velocity = savedVelocity;
    }

    private void Update()
    {
        if (isDestroyed)
            return;

        if (!isValid)
        {
            isDestroyed = true;
            Destroy(this);
            return;
        }

        try
        {
            UpdateResourceState();
            HandleOwnershipChange();
            HandleCycleReleasing();

            syncTimer -= UnityEngine.Time.unscaledDeltaTime;
            UpdateInterpolation();

            if (syncTimer >= 0)
                return;

            if (LocallyOwned)
                SendNetworkUpdate();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Update error: {ex}");
            isValid = false;
        }
    }

    private void UpdateResourceState()
    {
        if (!isResource || LocallyOwned || cycle == null || cycle._model == null || shouldUpdateResourceState)
            return;

        cycle._model.progressTime = double.MaxValue;
        shouldUpdateResourceState = false;
    }

    private void HandleOwnershipChange()
    {
        if (cachedLocallyOwned == LocallyOwned)
            return;

        SetRigidbodyState(LocallyOwned);

        if (LocallyOwned && rigidbody)
            rigidbody.velocity = savedVelocity;

        cachedLocallyOwned = LocallyOwned;
    }

    private void HandleCycleReleasing()
    {
        if (CycleReleasing == cachedCycleReleasing)
            return;

        cachedCycleReleasing = CycleReleasing;

        if (CycleReleasing != true)
            return;

        var actorId = ActorId;

        if (actorId.Value != 0)
            Main.SendToAllOrServer(new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID });
    }

    private void SendNetworkUpdate()
    {
        syncTimer = Timers.ActorTimer;
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        nextPosition = transform.position;
        nextRotation = transform.rotation;

        var actorId = ActorId;

        if (actorId.Value == 0)
            return;

        var packet = CreateActorUpdatePacket(actorId);
        Main.SendToAllOrServer(packet);
    }

    private ActorUpdatePacket CreateActorUpdatePacket(ActorId actorId)
    {
        var packet = new ActorUpdatePacket
        {
            ActorId = actorId,
            Position = nextPosition,
            Rotation = nextRotation,
            Velocity = rigidbody ? rigidbody.velocity : Vector3.zero
        };

        if (isSlime)
        {
            packet.UpdateType = ActorUpdateType.Slime;
            packet.Emotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
        }
        else if (isResource)
        {
            packet.UpdateType = ActorUpdateType.Resource;

            if (cycle?._model != null)
            {
                packet.ResourceProgress = cycle._model.progressTime;
                packet.ResourceState = cycle._model.state;
            }
        }
        else if (isPlort)
        {
            packet.UpdateType = ActorUpdateType.Plort;

            plortModel ??= GetComponent<PlortModel>();

            packet.Invulnerable = plortModel?._invulnerability?.IsInvulnerable ?? false;
            packet.InvulnerablePeriod = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;
        }
        else
        {
            packet.UpdateType = ActorUpdateType.Actor;
        }

        return packet;
    }

    private void SetRigidbodyState(bool enableConstraints)
    {
        if (rigidbody == null || isDestroyed)
            return;

        try
        {
            rigidbody.constraints = enableConstraints ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SetRigidbodyState error: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        isValid = false;
    }

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        if (cycle == null)
            return;

        shouldUpdateResourceState = true;

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