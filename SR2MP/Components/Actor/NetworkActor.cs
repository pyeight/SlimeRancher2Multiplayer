using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;
using Starlight.Storage;
using Unity.Mathematics;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;

namespace SR2MP.Components.Actor;

[InjectIntoIL]
internal sealed class NetworkActor : MonoBehaviour
{
    public RegionMember? RegionMember;

    private Identifiable identifiable;
    private ResourceCycle? cycle;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;
    private PlortModel? plortModel;

    public float SyncTimer = Timers.ActorTimer;
    public bool ShouldUpdateResourceState;
    public bool IsValid = true;
    public bool IsDestroyed;
    public byte AttemptedGetIdentifiable;
    public bool CachedLocallyOwned;

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

    private const int ForceSendInterval = 10;
    private int skippedUpdates;
    private bool staggerInitialized;

    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private Vector3 lastSentVelocity;
    private float4 lastSentEmotions;
    private bool lastSentSleeping;
    private double lastSentResourceProgress;
    private ResourceCycle.State lastSentResourceState;
    private bool lastSentInvulnerable;
    private float lastSentInvulnerablePeriod;

    public ActorId ActorId
    {
        get
        {
            if (IsDestroyed)
            {
                IsValid = false;
                return new ActorId(0);
            }

            if (identifiable != null)
                return GetActorIdSafe();

            if (AttemptedGetIdentifiable >= 10)
            {
                SrLogger.LogWarning("Failed to get Identifiable after 10 attempts");
                IsValid = false;
                return new ActorId(0);
            }

            try
            {
                identifiable = GetComponent<Identifiable>();
                AttemptedGetIdentifiable++;
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to get Identifiable component: {ex.Message}");
                AttemptedGetIdentifiable++;
                IsValid = false;
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
            IsValid = false;
            return new ActorId(0);
        }
    }

    public bool LocallyOwned { get; set; }

    public void Start()
    {
        try
        {
            if (GetComponent<Gadget>() || GetComponent<SRCharacterController>())
            {
                Destroy(this);
                return;
            }

            emotions     = GetComponent<SlimeEmotions>();
            rigidbody    = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();
            cycle        = GetComponent<ResourceCycle>();
            RegionMember = GetComponent<RegionMember>();

            CachedLocallyOwned = LocallyOwned;

            GetActorType();

            if (RegionMember != null)
                SetupHibernationEvent();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Start error: {ex}");
            IsValid = false;
        }
    }

    private void GetActorType()
    {
        if (ActorId.Value == 0 || !GameState.identifiables.TryGetValue(ActorId, out var identModel))
            return;

        isSlime    = identModel.TryCast<SlimeModel>()   != null;
        isResource = identModel.TryCast<ProduceModel>() != null;
        isPlort    = identModel.TryCast<PlortModel>()   != null;
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

            RegionMember?.add_BeforeHibernationChanged(hibernationDelegate.Cast<RegionMember.OnHibernationChange>());
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

        if (!IsValid || IsDestroyed)
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
            IsValid = false;
        }
    }

    public void HibernationChanged(bool value)
    {
        if (!IsValid || IsDestroyed)
            return;

        try
        {
            ContextShortcuts.StartCoroutine(WaitOneFrameOnHibernationChange(value));
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"HibernationChanged error: {ex}");
        }
    }

    public void OnNetworkUpdate(ActorUpdatePacket packet)
    {
        if (LocallyOwned || IsDestroyed)
            return;

        previousPosition   = transform.position;
        previousRotation   = transform.rotation;
        nextPosition       = packet.Position;
        nextRotation       = packet.Rotation;
        savedVelocity      = packet.Velocity;
        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd   = interpolationStart + Timers.ActorTimer;
    }

    private void UpdateInterpolation()
    {
        if (LocallyOwned || IsDestroyed || interpolationEnd <= interpolationStart)
            return;

        var t = Mathf.Clamp01(
            Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime));

        transform.position = Vector3.Lerp(previousPosition, nextPosition, t);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, t);

        if (rigidbody)
            rigidbody.velocity = savedVelocity;
    }

    public void Update()
    {
        if (IsDestroyed)
            return;

        if (!IsValid)
        {
            IsDestroyed = true;
            Destroy(this);
            return;
        }

        try
        {
            UpdateResourceState();
            HandleOwnershipChange();
            HandleCycleReleasing();

            SyncTimer -= UnityEngine.Time.unscaledDeltaTime;
            UpdateInterpolation();

            if (SyncTimer >= 0)
                return;

            if (LocallyOwned)
                SendNetworkUpdate();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Update error: {ex}");
            IsValid = false;
        }
    }

    private void UpdateResourceState()
    {
        if (!isResource || LocallyOwned || cycle == null || cycle._model == null || ShouldUpdateResourceState)
            return;

        cycle._model.progressTime = double.MaxValue;
        ShouldUpdateResourceState = false;
    }

    private void HandleOwnershipChange()
    {
        if (CachedLocallyOwned == LocallyOwned)
            return;

        SetRigidbodyState(LocallyOwned);

        if (LocallyOwned && rigidbody)
            rigidbody.velocity = savedVelocity;

        CachedLocallyOwned = LocallyOwned;
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
        SyncTimer = Timers.ActorTimer;

        var currentPosition = transform.position;
        var currentRotation = transform.rotation;
        var currentVelocity = rigidbody ? rigidbody.velocity : Vector3.zero;

        if (!staggerInitialized)
        {
            var id = ActorId;

            if (id.Value != 0)
            {
                skippedUpdates     = (int)(id.Value % ForceSendInterval);
                staggerInitialized = true;
            }
        }

        var changed = HasChanged(currentPosition, currentRotation, currentVelocity);

        if (!changed)
        {
            if (++skippedUpdates < ForceSendInterval)
                return;
        }

        skippedUpdates    = 0;
        lastSentPosition  = currentPosition;
        lastSentRotation  = currentRotation;
        lastSentVelocity  = currentVelocity;

        previousPosition  = currentPosition;
        previousRotation  = currentRotation;
        nextPosition      = currentPosition;
        nextRotation      = currentRotation;

        var actorId = ActorId;

        if (actorId.Value == 0)
            return;

        Main.SendToAllOrServer(CreateActorUpdatePacket(actorId));
    }

    private bool HasChanged(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        if (position != lastSentPosition || rotation != lastSentRotation || velocity != lastSentVelocity)
            return true;

        if (isSlime)
        {
            var currentEmotions = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
            var currentSleeping = emotions && emotions._model.isSleeping;
            return !currentEmotions.Equals(lastSentEmotions) || currentSleeping != lastSentSleeping;
        }

        if (isResource && cycle?._model != null)
        {
            return cycle._model.state != lastSentResourceState ||
                   cycle._model.progressTime != lastSentResourceProgress;
        }

        if (isPlort)
        {
            plortModel ??= GetComponent<PlortModel>();

            var invulnerable       = plortModel?._invulnerability?.IsInvulnerable    ?? false;
            var invulnerablePeriod = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;

            return invulnerable != lastSentInvulnerable || invulnerablePeriod != lastSentInvulnerablePeriod;
        }

        return false;
    }

    private ActorUpdatePacket CreateActorUpdatePacket(ActorId actorId)
    {
        var packet = new ActorUpdatePacket
        {
            ActorId  = actorId,
            Position = nextPosition,
            Rotation = nextRotation,
            Velocity = rigidbody ? rigidbody.velocity : Vector3.zero
        };

        if (isSlime)
        {
            packet.UpdateType = ActorUpdateType.Slime;
            packet.Emotions   = emotions ? emotions._model.Emotions : new float4(0, 0, 0, 0);
            packet.Sleeping   = emotions ? emotions._model.isSleeping : false;
            lastSentEmotions  = packet.Emotions;
            lastSentSleeping  = packet.Sleeping;
        }
        else if (isResource)
        {
            packet.UpdateType = ActorUpdateType.Resource;

            if (cycle?._model == null)
                return packet;

            packet.ResourceProgress   = cycle._model.progressTime;
            packet.ResourceState      = cycle._model.state;
            lastSentResourceProgress  = packet.ResourceProgress;
            lastSentResourceState     = packet.ResourceState;
        }
        else if (isPlort)
        {
            packet.UpdateType = ActorUpdateType.Plort;

            plortModel ??= GetComponent<PlortModel>();

            packet.Invulnerable          = plortModel?._invulnerability?.IsInvulnerable ?? false;
            packet.InvulnerablePeriod    = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;
            lastSentInvulnerable         = packet.Invulnerable;
            lastSentInvulnerablePeriod   = packet.InvulnerablePeriod;
        }
        else
        {
            packet.UpdateType = ActorUpdateType.Actor;
        }

        return packet;
    }

    private void SetRigidbodyState(bool locallyOwned)
    {
        if (rigidbody == null || IsDestroyed)
            return;

        try
        {
            rigidbody.constraints = locallyOwned 
                ? RigidbodyConstraints.None 
                : RigidbodyConstraints.FreezeAll;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SetRigidbodyState error: {ex.Message}");
        }
    }

    [UsedImplicitly]
    public void OnDestroy()
    {
        IsDestroyed = true;
        IsValid     = false;
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