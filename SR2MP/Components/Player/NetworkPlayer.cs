using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;
using Il2CppMonomiPark.SlimeRancher.Player.PlayerItems;
using Il2CppMonomiPark.SlimeRancher.UI;
using Il2CppTMPro;
using JetBrains.Annotations;
using MelonLoader;
using SR2E.Utils;
using SR2MP.Client.Models;
using SR2MP.Components.FX;
using SR2MP.Components.Utils;
using SR2MP.Shared.Managers;

using static SR2E.ContextShortcuts;
using static SR2MP.Shared.Utils.Timers;

namespace SR2MP.Components.Player;

[RegisterTypeInIl2Cpp(false)]
//[RegisterTypeInIl2CppWithInterfaces(false, typeof(IMapMarkerSource))]
internal partial class NetworkPlayer : MonoBehaviour
{
    private static readonly int HorizontalMovement = Animator.StringToHash("HorizontalMovement");
    private static readonly int ForwardMovement = Animator.StringToHash("ForwardMovement");
    private static readonly int Yaw = Animator.StringToHash("Yaw");
    private static readonly int AirborneState = Animator.StringToHash("AirborneState");
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private static readonly int ForwardSpeed = Animator.StringToHash("ForwardSpeed");
    private static readonly int Sprinting = Animator.StringToHash("Sprinting");

    // private MeshRenderer[] renderers;
    private Collider collider;

    public int previousScene;
    
    public Vector3 previousPosition;
    public Vector3 nextPosition;

    public Vector2 previousRotation;
    public Vector2 nextRotation;

    private float interpolationStart;
    private float interpolationEnd;

    public TextMeshPro UsernamePanel;

    private float transformTimer = PlayerTimer;

    private Animator animator;
    private bool hasAnimationController;

    private RemotePlayer? model;

    public Transform camera;

    public string ID { get; internal set; }

    public bool IsLocal { get; internal set; }

    private static TMP_FontAsset GetFont(string fontName) => Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(x => x.name == fontName)!;

    internal TMP_FontAsset usernameFont;
    
    public void SetUsername(string username)
    {
        username = username.Trim();

        UsernamePanel = transform.GetChild(1).GetComponent<TextMeshPro>();
        UsernamePanel.text = username;
        UsernamePanel.alignment = TextAlignmentOptions.Center;
        UsernamePanel.fontSize = 3;
        UsernamePanel.font = GetFont("Runsell Type - HemispheresCaps2 (Latin)");

        usernameFont = UsernamePanel.font;
        
        if (!UsernamePanel.GetComponent<TransformLookAtCamera>())
        {
            UsernamePanel.gameObject.AddComponent<TransformLookAtCamera>().TargetTransform =
                UsernamePanel.transform;
        }
        
        GetComponent<RadarTrackedPointOfInterest>()
            ._compassRadarPrefab
            .transform
            .GetChild(0)
            .GetComponent<TextMeshProUGUI>()
            .SetText(username);
        GetComponent<RadarTrackedPointOfInterest>().enabled = true;
    }

    [UsedImplicitly]
    public void Awake()
    {
        if (transform.GetComponents<NetworkPlayer>().Length > 1)
        {
            Destroy(this);
            return;
        }

        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            SrLogger.LogWarning("NetworkPlayer has no Animator component!");
        }
        AwakeGadgetMode();
    }

    public void Start()
    {
        if (IsLocal)
        {
            camera = GetComponent<SRCharacterController>()._cameraController.transform;
            GetComponent<PlayerItemController>()._vacuumItem.AddComponent<NetworkPlayerSound>();
        }
        else
        {
            PlayerMarkerTransforms[ID] = new();
        }

        UsernamePanel = transform.GetChild(1).GetComponent<TextMeshPro>();

        SetupRenderersAndCollision();
    }

    private void SetupRenderersAndCollision()
    {
        // if (IsLocal)
        // {
        //     var modelRenderers = GetComponentsInChildren<MeshRenderer>();
        //     var cameraRenderers = camera.GetComponentsInChildren<MeshRenderer>();
        //     var allRenderers = new MeshRenderer[modelRenderers.Length + cameraRenderers.Length];

        //     modelRenderers.CopyTo(allRenderers, 0);
        //     cameraRenderers.CopyTo(allRenderers, modelRenderers.Length);

        //     renderers = allRenderers;
        // }
        // else
        // {
        //     renderers = GetComponentsInChildren<MeshRenderer>();
        // }

        collider = GetComponentInChildren<Collider>();
    }

    public void Update()
    {
        if (model == null)
        {
            model = PlayerManager.GetPlayer(ID) ?? PlayerManager.AddPlayer(ID);

            if (!UsernamePanel)
                return;

            UsernamePanel.gameObject.AddComponent<TransformLookAtCamera>().TargetTransform =
                UsernamePanel.transform;

            SetupMarker();
            SetUsername(model.Username);

            return;
        }

        transformTimer -= UnityEngine.Time.unscaledDeltaTime;

        if (!IsLocal)
        {
            var timer = Mathf.InverseLerp(interpolationStart, interpolationEnd, UnityEngine.Time.unscaledTime);

            var networkPosition = Vector3.LerpUnclamped(previousPosition, nextPosition, timer);
            var networkLookY = Mathf.LerpAngle(previousRotation.y, nextRotation.y, timer);
            var networkYaw = Mathf.LerpAngle(previousRotation.x, nextRotation.x, timer);

            if (Vector3.SqrMagnitude(transform.position - networkPosition) > 9f)
            {
                transform.position = networkPosition;
                transform.eulerAngles = new Vector3(0, networkYaw, 0);
                ReceivedLookY = networkLookY;
            }
            else
            {
                var blendSpeed = UnityEngine.Time.unscaledDeltaTime * 15f;

                transform.position = Vector3.Lerp(transform.position, networkPosition, blendSpeed);

                ReceivedLookY = Mathf.LerpAngle(ReceivedLookY, networkLookY, blendSpeed);

                var targetRot = Quaternion.Euler(0, networkYaw, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, blendSpeed);
            }
        }

        ReloadMeshTransform();

        UpdateGadgetMode();

        UpdateMarker();
        
        if (transformTimer >= 0f)
            return;

        transformTimer = PlayerTimer;

        if (IsLocal)
        {
            UpdateLocalGadgetMode();

            RemotePlayerManager.SendPlayerUpdate(
                position: transform.position,
                rotation: transform.eulerAngles.y,
                horizontalMovement: animator.GetFloat(HorizontalMovement),
                forwardMovement: animator.GetFloat(ForwardMovement),
                yaw: animator.GetFloat(Yaw),
                airborneState: animator.GetInteger(AirborneState),
                moving: animator.GetBool(Moving),
                horizontalSpeed: animator.GetFloat(HorizontalSpeed),
                forwardSpeed: animator.GetFloat(ForwardSpeed),
                sprinting: animator.GetBool(Sprinting),
                lookY: camera.eulerAngles.x,
                sceneGroup: NetworkSceneManager.GetPersistentID(SystemContext.Instance.SceneLoader._currentSceneGroup)
            );
        }
        else
        {
            if (!hasAnimationController)
            {
                var playerAnimatorController = sceneContext.player?.GetComponent<Animator>().runtimeAnimatorController;

                if (animator.runtimeAnimatorController != null)
                {
                    hasAnimationController = true;
                    animator.runtimeAnimatorController =
                        Instantiate(playerAnimatorController);
                    animator.avatar = sceneContext.player?.GetComponent<Animator>().avatar;
                    SetupAnimations();
                }
            }

            previousPosition = nextPosition;
            nextPosition = model.Position;

            previousRotation = new Vector2(transform.eulerAngles.y, model.LastLookY);
            nextRotation = new Vector2(model.Rotation, model.LookY);

            interpolationStart = UnityEngine.Time.unscaledTime;
            interpolationEnd = UnityEngine.Time.unscaledTime + PlayerTimer;

            animator.SetFloat(HorizontalMovement, model.HorizontalMovement);
            animator.SetFloat(ForwardMovement, model.ForwardMovement);
            animator.SetFloat(Yaw, model.Yaw);
            animator.SetInteger(AirborneState, model.AirborneState);
            animator.SetBool(Moving, model.Moving);
            animator.SetFloat(HorizontalSpeed, model.HorizontalSpeed);
            animator.SetFloat(ForwardSpeed, model.ForwardSpeed);
            animator.SetBool(Sprinting, model.Sprinting);
        }
    }

    private void ReloadMeshTransform()
    {
        // foreach (var renderer in renderers)
        // {
        //     // This is for the getter to refresh the render position stuff qwq
        //     var bounds = renderer.bounds;
        //     var localBounds = renderer.localBounds;
        // }

        if (IsLocal)
            return;

        collider.enabled = false;
        collider.enabled = true;
    }

    [UsedImplicitly]
    public void LateUpdate() => AnimateArmY();
}