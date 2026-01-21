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
    internal RegionMember? RegionMember;
    private IdentifiableActor identifiableActor;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;

    private float syncTimer = Timers.ActorTimer;
    public Vector3 SavedVelocity { get; internal set; }

    private byte attemptedGetIdentifiable;

    private ActorId ActorId
    {
        get
        {
            if (identifiableActor)
                return identifiableActor._model.actorId;

            identifiableActor = GetComponent<IdentifiableActor>();
            attemptedGetIdentifiable++;

            if (attemptedGetIdentifiable >= 10)
            {
                Destroy(this);
            }

            return new ActorId(0);
        }
    }

    public bool LocallyOwned { get; set; }
    private bool cachedLocallyOwned;

    internal Vector3 PreviousPosition;
    internal Vector3 NextPosition;

    internal Quaternion PreviousRotation;
    internal Quaternion NextRotation;

    // Uncomment when these are ACTUALLY used!
    // private Vector3 previousSentPosition;
    // private const float MinimumPositionDifference = 0.15f;

    private float interpolationStart;
    private float interpolationEnd;

    private float4 EmotionsFloat => emotions
                                    ? emotions._model.Emotions
                                    : new float4(0, 0, 0, 0);

    public void Start()
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
        identifiableActor = GetComponent<IdentifiableActor>();

        RegionMember = GetComponent<RegionMember>();

        RegionMember.add_BeforeHibernationChanged(
            Delegate.CreateDelegate(Type.GetType("MonomiPark.SlimeRancher.Regions.RegionMember")
                    .GetEvent("BeforeHibernationChanged").EventHandlerType,
                this.Cast<Il2CppSystem.Object>(),
                nameof(HibernationChanged),
                true)
                .Cast<RegionMember.OnHibernationChange>());
    }

    IEnumerator WaitOneFrameOnHibernationChange(bool value)
    {
        yield return null;

        if (value)
        {
            LocallyOwned = false;

            var packet = new ActorUnloadPacket { ActorId = ActorId };
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
        MelonCoroutines.Start(WaitOneFrameOnHibernationChange(value));
    }

    private void UpdateInterpolation()
    {
        if (LocallyOwned) return;

        var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        timer = Mathf.Clamp01(timer);

        transform.position = Vector3.Lerp(PreviousPosition, NextPosition, timer);
        transform.rotation = Quaternion.Lerp(PreviousRotation, NextRotation, timer);
    }

    public void Update()
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

            // previousSentPosition = transform.position;

            PreviousPosition = transform.position;
            PreviousRotation = transform.rotation;
            NextPosition = transform.position;
            NextRotation = transform.rotation;

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
            PreviousPosition = transform.position;
            PreviousRotation = transform.rotation;

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