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
    
    private bool shouldUpdateResourceState;

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
                    return new ActorId(0);
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

    private void Start()
    {
        try
        {
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
            
            if (ActorId.Value != 0 && GameState.identifiables.TryGetValue(ActorId, out var identModel))
            {
                isSlime = identModel.TryCast<SlimeModel>() != null;
                isResource = identModel.TryCast<ProduceModel>() != null;
                isPlort = identModel.TryCast<PlortModel>() != null;
            }

            emotions = GetComponent<SlimeEmotions>();
            cachedLocallyOwned = LocallyOwned;
            rigidbody = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();
            cycle = GetComponent<ResourceCycle>();
            regionMember = GetComponent<RegionMember>();

            if (!regionMember) return;

            try
            {
                regionMember.add_BeforeHibernationChanged(
                    Delegate.CreateDelegate(
                        Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
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
            yield break;

        try
        {
            if (value)
            {
                LocallyOwned = false;
                var actorId = ActorId;
                if (actorId.Value == 0) yield break;

                var packet = new ActorUnloadPacket { ActorId = actorId };
                Main.SendToAllOrServer(packet);
            }
            else
            {
                LocallyOwned = true;
                var actorId = ActorId;
                if (actorId.Value == 0) yield break;

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
        if (!isValid || isDestroyed) return;

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
        if (LocallyOwned || isDestroyed) return;
        
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
        if (interpolationEnd <= interpolationStart) return;

        var t = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        t = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, t);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, t);
        
        if (rigidbody)
            rigidbody.velocity = SavedVelocity;
    }

    private void Update()
    {
        if (isDestroyed) return;

        if (!isValid)
        {
            isDestroyed = true;
            Destroy(this);
            return;
        }
        
        if (isResource && !LocallyOwned && cycle != null && cycle._model != null && !shouldUpdateResourceState)
            cycle._model.progressTime = double.MaxValue;
        
        if (shouldUpdateResourceState)
            shouldUpdateResourceState = false;

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
                      isSlime ? ActorUpdateType.Slime
                    : isResource ? ActorUpdateType.Resource
                    : isPlort ? ActorUpdateType.Plort
                    : ActorUpdateType.Actor;

                double resourceProgress = 0f;
                ResourceCycle.State resourceState = ResourceCycle.State.UNRIPE;
                if (isResource && cycle != null && cycle._model != null)
                {
                    resourceProgress = cycle._model.progressTime;
                    resourceState = cycle._model.state;
                }

                var packet = new ActorUpdatePacket();
                packet.UpdateType = updateType;

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
                    var plortModel = GetComponent<PlortModel>();
                    packet.ActorId = actorId;
                    packet.Position = transform.position;
                    packet.Rotation = transform.rotation;
                    packet.Velocity = rigidbody ? rigidbody.velocity : Vector3.zero;
                    packet.Invulnerable = plortModel?._invulnerability?.IsInvulnerable ?? false;
                    packet.InvulnerablePeriod = plortModel?._invulnerability?.InvulnerabilityPeriod ?? 0f;
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
        if (!rigidbody || isDestroyed) return;

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

    private ResourceCycle.State? prevState;

    public void SetResourceState(ResourceCycle.State state, double progress, bool force = false)
    {
        if (!cycle || cycle == null) return;
        
        shouldUpdateResourceState = true;
        
        if (cycle._model != null)
            cycle._model.progressTime = progress;
        
        if (!force && prevState == state) return;
        prevState = state;

        try
        {
            if (cycle._model != null)
                cycle._model.state = state;

            switch (state)
            {
                case ResourceCycle.State.UNRIPE:
                {
                    if (gameObject.transform.localScale.x < cycle._defaultScale.x * 0.33f)
                        gameObject.transform.localScale = cycle._defaultScale * 0.33f;

                    if (cycle._vacuumable)
                        cycle._vacuumable.enabled = false;

                    if (rigidbody && cycle._joint != null)
                        rigidbody.isKinematic = true;

                    break;
                }
                case ResourceCycle.State.RIPE:
                {
                    if (cycle._vacuumable)
                        cycle._vacuumable.enabled = true;

                    if (gameObject.transform.localScale.x < cycle._defaultScale.x)
                        gameObject.transform.localScale = cycle._defaultScale;

                    if (cycle._joint != null)
                    {
                        if (rigidbody)
                        {
                            rigidbody.isKinematic = false;
                            rigidbody.WakeUp();
                        }
                        cycle.DetachFromJoint();
                    }

                    break;
                }
                case ResourceCycle.State.EDIBLE:
                {
                    if (cycle._vacuumable)
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

                    break;
                }
                case ResourceCycle.State.ROTTEN:
                {
                    cycle.SetRotten(false);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"SetResourceState error: {ex}", SrLogTarget.Both);
        }
    }
}