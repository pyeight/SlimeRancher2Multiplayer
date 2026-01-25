using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using System.Collections;
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

    private ActorId ActorId
    {
        get
        {
            if (!identifiable)
            {
                identifiable = GetComponent<Identifiable>();
                attemptedGetIdentifiable++;

                if (attemptedGetIdentifiable >= 10)
                {
                    isValid = false;
                }

                return new ActorId(0);
            }
            
            try
            {
                return identifiable.GetActorId();
            }
            catch
            {
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
            regionMember.add_BeforeHibernationChanged(
                Delegate.CreateDelegate(Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                        .GetEvent("BeforeHibernationChanged").EventHandlerType,
                    this.Cast<Il2CppSystem.Object>(),
                    nameof(HibernationChanged),
                    true)
                    .Cast<RegionMember.OnHibernationChange>());
        }
    }

    IEnumerator WaitOneFrameOnHibernationChange(bool value)
    {
        yield return null;

        if (!isValid) yield break;

        if (value)
        {
            LocallyOwned = false;

            var packet = new ActorUnloadPacket() { ActorId = ActorId };
            Main.SendToAllOrServer(packet);
        }
        else
        {
            LocallyOwned = true;

            var packet = new ActorTransferPacket
            {
                ActorId = ActorId,
                OwnerPlayer = LocalID,
            };
            Main.SendToAllOrServer(packet);
        }
    }

    public void HibernationChanged(bool value)
    {
        if (!isValid) return;
        MelonCoroutines.Start(WaitOneFrameOnHibernationChange(value));
    }

    private void UpdateInterpolation()
    {
        if (LocallyOwned) return;

        var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        timer = Mathf.Clamp01(timer);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, timer);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, timer);
    }

    private void Update()
    {
        if (!isValid)
        {
            Destroy(this);
            return;
        }

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

            var packet = new ActorUpdatePacket
            {
                ActorId = ActorId,
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

    private void SetRigidbodyState(bool enableConstraints)
    {
        if (!rigidbody)
            return;

        rigidbody.constraints =
            enableConstraints
                ? RigidbodyConstraints.None
                : RigidbodyConstraints.FreezeAll;
    }
}