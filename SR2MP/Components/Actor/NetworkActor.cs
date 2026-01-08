using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using System.Collections;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;
using Unity.Mathematics;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;

namespace SR2MP.Components.Actor;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkActor : MonoBehaviour
{
    private RegionMember regionMember;
    private IdentifiableActor identifiableActor;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;

    private float syncTimer = Timers.ActorTimer;
    public Vector3 SavedVelocity { get; internal set; }

    private byte attemptedGetIdentifiable = 0;
    
    private ActorId ActorId
    {
        get
        {
            if (!identifiableActor)
            {
                identifiableActor = GetComponent<IdentifiableActor>();
                attemptedGetIdentifiable++;

                if (attemptedGetIdentifiable >= 6)
                {
                    Destroy(this);
                }

                return new ActorId(0);
            }

            return identifiableActor._model.actorId;
        }
    }

    public bool LocallyOwned { get; set; }
    private bool cachedLocallyOwned;

    internal Vector3 previousPosition;
    internal Vector3 nextPosition;

    internal Quaternion previousRotation;
    internal Quaternion nextRotation;

    private Vector3 previousSentPosition;
    private const float MinimumPositionDifference = 0.15f;

    private float interpolationStart;
    private float interpolationEnd;

    private float4 EmotionsFloat => emotions
                                    ? emotions._model.Emotions
                                    : new float4(0, 0, 0, 0);

    void Start()
    {
        emotions = GetComponent<SlimeEmotions>();
        cachedLocallyOwned = LocallyOwned;
        rigidbody = GetComponent<Rigidbody>();
        identifiableActor = GetComponent<IdentifiableActor>();

        regionMember = GetComponent<RegionMember>();

        regionMember.add_BeforeHibernationChanged(
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

        if (!value) yield break;

        LocallyOwned = true;

        var packet = new ActorTransferPacket
        {
            Type = (byte)PacketType.ActorTransfer,
            ActorId = ActorId,
            OwnerPlayer = LocalID,
        };
        Main.SendToAllOrServer(packet);
    }

    public void HibernationChanged(bool value)
    {
        MelonCoroutines.Start(WaitOneFrameOnHibernationChange(value));
    }

    void UpdateInterpolation()
    {
        if (LocallyOwned) return;

        var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);
        timer = Mathf.Clamp01(timer);

        transform.position = Vector3.Lerp(previousPosition, nextPosition, timer);
        transform.rotation = Quaternion.Lerp(previousRotation, nextRotation, timer);
    }

    void Update()
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

            previousSentPosition = transform.position;

            previousPosition = transform.position;
            previousRotation = transform.rotation;
            nextPosition = transform.position;
            nextRotation = transform.rotation;

            var packet = new ActorUpdatePacket
            {
                Type = (byte)PacketType.ActorUpdate,
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