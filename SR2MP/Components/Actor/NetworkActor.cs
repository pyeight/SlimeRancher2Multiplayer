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
    internal RegionMember regionMember;
    private Identifiable identifiable;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;

    private float syncTimer = Timers.ActorTimer;
    public Vector3 SavedVelocity { get; internal set; }

    private byte attemptedGetIdentifiable = 0;
    private bool isValid = true;
    private bool isDestroyed = false;

    public ActorId ActorId
    {
        get
        {
            // FIXED: Check if this component or gameObject is destroyed first
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

            emotions = GetComponent<SlimeEmotions>();
            cachedLocallyOwned = LocallyOwned;
            rigidbody = GetComponent<Rigidbody>();
            identifiable = GetComponent<Identifiable>();

            regionMember = GetComponent<RegionMember>();

            if (regionMember)
            {
                try
                {
                    regionMember.add_BeforeHibernationChanged(
                        Delegate.CreateDelegate(Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                                .GetEvent("BeforeHibernationChanged").EventHandlerType,
                            this.Cast<Il2CppSystem.Object>(),
                            nameof(HibernationChanged),
                            true)
                            .Cast<RegionMember.OnHibernationChange>());
                }
                catch (Exception ex)
                {
                    SrLogger.LogWarning($"Failed to add hibernation event: {ex.Message}", SrLogTarget.Both);
                }
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

                var packet = new ActorTransferPacket
                {
                    ActorId = actorId,
                    OwnerPlayer = LocalID,
                };
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

    private void UpdateInterpolation()
    {
        if (LocallyOwned) return;
        if (isDestroyed) return;

        var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        timer = Mathf.Clamp01(timer);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, timer);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, timer);
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
                if (actorId.Value == 0)
                {
                    return;
                }

                var packet = new ActorUpdatePacket
                {
                    ActorId = actorId,
                    Position = transform.position,
                    Rotation = transform.rotation,
                    Velocity = rigidbody ? rigidbody.velocity : Vector3.zero,
                    Emotions = EmotionsFloat
                };

                Main.SendToAllOrServer(packet);
            }
            else
            {
                previousPosition = transform.position;
                previousRotation = transform.rotation;

                interpolationStart = UnityEngine.Time.unscaledTime;
                interpolationEnd = UnityEngine.Time.unscaledTime + Timers.ActorTimer;
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
}