using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.World;
using MelonLoader;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;
using Unity.Mathematics;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;

namespace SR2MP.Components.Actor;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkActor : MonoBehaviour
{
    internal RegionMember? regionMember;
    private Identifiable identifiable;
    private ResourceCycle? cycle;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;

    private float syncTimer = Timers.ActorTimer;
    public Vector3 SavedVelocity { get; internal set; }

    private byte attemptedGetIdentifiable;
    private bool isValid = true;
    private bool isDestroyed;

    private bool? cachedCycleReleasing;
    public bool? CycleReleasing => cycle?._preparingToRelease;

    public ActorId ActorId
    {
        get
        {
            if (isDestroyed)
            {
                isValid = false;
                return new ActorId(0);
            }

            if (!identifiable)
            {
                try
                {
                    identifiable = GetComponent<Identifiable>();
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Failed to get Identifiable component: {ex.Message}", SrLogTarget.Both);
                    isValid = false;
                    return new ActorId(0);
                }

                attemptedGetIdentifiable++;

                if (attemptedGetIdentifiable >= 10)
                {
                    SrLogger.LogWarning("Failed to get Identifiable after 10 attempts", SrLogTarget.Both);
                    isValid = false;
                }

                if (!identifiable)
                {
                    return new ActorId(0);
                }
            }

            try
            {
                return identifiable.GetActorId();
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to get ActorId: {ex.Message}", SrLogTarget.Both);
                isValid = false;
                return new ActorId(0);
            }
        }
    }

    public bool LocallyOwned { get; set; }
    private bool cachedLocallyOwned;

    internal Vector3 previousPosition;
    internal Vector3 nextPosition;

    internal Quaternion previousRotation;
    internal Quaternion nextRotation;

    private float interpolationStart;
    private float interpolationEnd;

    private float4 EmotionsFloat => emotions
        ? emotions._model.Emotions
        : new float4(0, 0, 0, 0);

    private bool isSlime;
    private bool isResource;
    private bool isPlort;

    private SlimeModel slimeModel;
    private ProduceModel produceModel;
    private PlortModel plortModel;

    private void Start()
    {
        try
        {
            // Check for components that shouldn't have NetworkActor
            if (GetComponent<Gadget>())
            {
                Destroy(this);
                return;
            }

            if (GetComponent<SRCharacterController>())
            {
                Destroy(this);
                return;
            }
            
            isSlime = TryGetComponent<SlimeModel>(out slimeModel);
            isResource = TryGetComponent<ProduceModel>(out produceModel);
            isPlort = TryGetComponent<PlortModel>(out plortModel);

            emotions = GetComponent<SlimeEmotions>();
            cachedLocallyOwned = LocallyOwned;
            rigidbody = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();
            cycle = GetComponent<ResourceCycle>();

            regionMember = GetComponent<RegionMember>();

            if (!regionMember)
                return;
            
            try
            {
                regionMember.add_BeforeHibernationChanged(
                    Delegate.CreateDelegate(Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                                .GetEvent("BeforeHibernationChanged").EventHandlerType,
                            Cast<Il2CppSystem.Object>(),
                            nameof(HibernationChanged),
                            true)
                        .Cast<RegionMember.OnHibernationChange>());
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to add hibernation event: {ex.Message}", SrLogTarget.Both);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Start error: {ex}", SrLogTarget.Both);
            isValid = false;
        }
    }

    [HideFromIl2Cpp]
    private IEnumerator WaitOneFrameOnHibernationChange(bool value)
    {
        yield return null;

        if (!isValid || isDestroyed)
        {
            yield break;
        }

        try
        {
            if (value)
            {
                LocallyOwned = false;

                var actorId = ActorId;
                if (actorId.Value == 0)
                {
                    yield break;
                }

                var packet = new ActorUnloadPacket { ActorId = actorId };
                Main.SendToAllOrServer(packet);
            }
            else
            {
                LocallyOwned = true;

                var actorId = ActorId;
                if (actorId.Value == 0)
                {
                    yield break;
                }

                var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
                Main.SendToAllOrServer(packet);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"WaitOneFrameOnHibernationChange error: {ex}", SrLogTarget.Both);
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
            SrLogger.LogError($"HibernationChanged error: {ex}", SrLogTarget.Both);
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
        SavedVelocity = packet.Velocity;
        
        interpolationStart = UnityEngine.Time.unscaledTime;
        interpolationEnd = interpolationStart + Timers.ActorTimer;
    }

    private void UpdateInterpolation()
    {
        if (LocallyOwned) return;
        if (isDestroyed) return;
        
        if (interpolationEnd <= interpolationStart)
            return;

        var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, t);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, t);
        
        if (rigidbody)
            rigidbody.velocity = SavedVelocity;
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

        if (CycleReleasing != cachedCycleReleasing)
        {
            cachedCycleReleasing = CycleReleasing;
            if (CycleReleasing == true)
            {
                var actorId = ActorId;
                if (actorId.Value != 0)
                {
                    var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
                    Main.SendToAllOrServer(packet);
                }
            }
        }

        try
        {
            if (cachedLocallyOwned != LocallyOwned)
            {
                SetRigidbodyState(LocallyOwned);

                if (LocallyOwned && rigidbody)
                    rigidbody.velocity = SavedVelocity;
            }

            cachedLocallyOwned = LocallyOwned;
            syncTimer -= UnityEngine.Time.unscaledDeltaTime;

            UpdateInterpolation();

            if (syncTimer >= 0) return;

            if (LocallyOwned)
            {
                syncTimer = Timers.ActorTimer;

                previousPosition = transform.position;
                previousRotation = transform.rotation;
                nextPosition = transform.position;
                nextRotation = transform.rotation;

                var actorId = ActorId;
                if (actorId.Value == 0) return;
                
                ActorUpdateType updateType =
                    isSlime
                    ? ActorUpdateType.Slime
                    : isResource
                        ? ActorUpdateType.Resource
                        : isPlort
                            ? ActorUpdateType.Plort
                            : ActorUpdateType.Actor;
                
                double resourceProgress = 0f;
                ResourceCycle.State resourceState = ResourceCycle.State.UNRIPE;
                if (isResource)
                {
                    resourceProgress = cycle?._model.progressTime ?? 0f;
                    resourceState = cycle?._model.state ?? ResourceCycle.State.UNRIPE;
                }

                var packet = new ActorUpdatePacket()
                {
                    UpdateType = updateType,
                };
                
                if (updateType == ActorUpdateType.Slime)
                {
                    packet.ActorId = actorId;
                    packet.Position = transform.position;
                    packet.Rotation = transform.rotation;
                    packet.Velocity = rigidbody ? rigidbody.velocity : Vector3.zero;
                    packet.Emotions = EmotionsFloat;
                }
                else if (updateType == ActorUpdateType.Resource)
                {
                    packet.ActorId = actorId;
                    packet.Position = transform.position;
                    packet.Rotation = transform.rotation;
                    packet.Velocity = rigidbody ? rigidbody.velocity : Vector3.zero;
                    packet.ResourceProgress = resourceProgress;
                    packet.ResourceState = resourceState;
                }
                else if (updateType == ActorUpdateType.Plort)
                {
                    packet.ActorId = actorId;
                    packet.Position = transform.position;
                    packet.Rotation = transform.rotation;
                    packet.Velocity = rigidbody ? rigidbody.velocity : Vector3.zero;
                    // todo: packet.Invulnerable = plortModel._invulnerability?.IsInvulnerable ?? false; ??
                    // todo: packet.InvulnerablePeriod = plortModel._invulnerability?.InvulnerabilityPeriod ?? 0f; ??
                    packet.Invulnerable = plortModel._invulnerability?.IsInvulnerable ?? false;
                    packet.InvulnerablePeriod = plortModel._invulnerability?.InvulnerabilityPeriod ?? 0f;
                }
                else
                {
                    packet.ActorId = actorId;
                    packet.Position = transform.position;
                    packet.Rotation = transform.rotation;
                    packet.Velocity = rigidbody ? rigidbody.velocity : Vector3.zero;
                }

                Main.SendToAllOrServer(packet);
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Update error: {ex}", SrLogTarget.Both);
            isValid = false;
        }
    }

    private void SetRigidbodyState(bool enableConstraints)
    {
        if (!rigidbody || isDestroyed)
            return;

        try
        {
            rigidbody.constraints =
                enableConstraints
                    ? RigidbodyConstraints.None
                    : RigidbodyConstraints.FreezeAll;
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SetRigidbodyState error: {ex.Message}", SrLogTarget.Both);
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        isValid = false;
    }

    private ResourceCycle.State prevState;

    public void SetResourceState(ResourceCycle.State state, double progress)
    {
        if (prevState == state)
            return;
        if (!cycle)
            return;
        switch (state)
        {
            case ResourceCycle.State.UNRIPE:
            {
                cycle!._vacuumable.enabled = false;

                if (base.gameObject.transform.localScale.x < cycle._defaultScale.x * 0.33f)
                {
                    base.gameObject.transform.localScale = cycle._defaultScale * 0.33f;
                }
                break;
            }
            case ResourceCycle.State.RIPE:
            {
                cycle!.Ripen();

                cycle._vacuumable.enabled = true;

                if (base.gameObject.transform.localScale.x < cycle._defaultScale.x)
                {
                    base.gameObject.transform.localScale = cycle._defaultScale;
                }

                rigidbody.WakeUp();
                cycle.Eject(rigidbody);
                cycle.DetachFromJoint();

                TweenUtil.ScaleTo(base.gameObject, cycle._defaultScale, 4f);
                break;
            }
            case ResourceCycle.State.EDIBLE:
            {
                cycle!.MakeEdible();

                cycle._additionalRipenessDelegate = null;
                rigidbody.isKinematic = false;

                cycle._vacuumable.enabled = true;

                if (cycle._preparingToRelease)
                {
                    // todo: cycle._preparingToRelease = false;
                    // todo: cycle._releaseAt = 0f;
                    cycle.ToShake.localPosition = cycle._toShakeDefaultPos;

                    if (cycle.ReleaseCue != null)
                    {
                        var audio = base.GetComponent<SECTR_PointSource>();
                        audio.Cue = cycle.ReleaseCue;
                        audio.Play();
                    }
                }

                rigidbody.WakeUp();
                cycle.Eject(rigidbody);
                cycle.DetachFromJoint();
                if (cycle._hasVacuumable)
                {
                    cycle._vacuumable.Pending = false;
                }
                break;
            }
            case ResourceCycle.State.ROTTEN:
            {
                cycle!.Rot();
                cycle.SetRotten(false);
                break;
            }
        }

        cycle!._model.progressTime = progress;
        prevState = state;
    }
}