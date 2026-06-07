using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.Slime;
using JetBrains.Annotations;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Utils;
using Starlight.Storage;
using Delegate = Il2CppSystem.Delegate;
using Type = Il2CppSystem.Type;

namespace SR2MP.Components.Actor;

[InjectIntoIL]
internal sealed partial class NetworkActor : MonoBehaviour
{
    public  RegionMember? RegionMember;
    private Identifiable identifiable;
    private ResourceCycle? cycle;
    private Rigidbody rigidbody;
    private SlimeEmotions emotions;
    private PlortModel? plortModel;

    public float SyncTimer = Timers.ActorTimer;
    public bool  ShouldUpdateResourceState;
    public bool  IsValid = true;
    public bool  IsDestroyed;
    public byte  AttemptedGetIdentifiable;
    public bool  CachedLocallyOwned;

    private bool? CycleReleasing => cycle?._preparingToRelease;
    private bool? cachedCycleReleasing;

    private Vector3 savedVelocity;

    private bool isSlime;
    private bool isResource;
    private bool isPlort;

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
            
            SetRigidbodyState(LocallyOwned);

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

            UpdatePolation();

            if (LocallyOwned)
                SendStateUpdate();

            SyncTimer -= UnityEngine.Time.unscaledDeltaTime;

            if (SyncTimer >= 0)
                return;

            SyncTimer = Timers.ActorTimer;

            if (LocallyOwned)
                SendWorldUpdate();
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"NetworkActor.Update error: {ex}");
            IsValid = false;
        }
    }

    private void HandleOwnershipChange()
    {
        if (CachedLocallyOwned != LocallyOwned)
        {
            SetRigidbodyState(LocallyOwned);

            if (LocallyOwned && rigidbody)
                rigidbody.velocity = savedVelocity;
        }

        CachedLocallyOwned = LocallyOwned;
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
}