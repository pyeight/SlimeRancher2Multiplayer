using MelonLoader;
using UnityEngine;
using SR2MP.Client.Models;
using static SR2E.ContextShortcuts;
using static SR2MP.Shared.Utils.Timers;

namespace SR2MP.Components
{
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkPlayer : MonoBehaviour
    {
        public TextMesh usernamePanel;
        private float transformTimer = PlayerTimer;
        private Animator animator;
        private Transform cachedTransform;

        public void SetUsername(string username)
        {
            usernamePanel = transform.GetChild(1).GetComponent<TextMesh>();
            usernamePanel.text = username;
            usernamePanel.characterSize = 0.2f;
            usernamePanel.anchor = TextAnchor.MiddleCenter;
            usernamePanel.fontSize = 24;
            if (!usernamePanel.GetComponent<TransformLookAtCamera>())
            {
                usernamePanel.gameObject.AddComponent<TransformLookAtCamera>().targetTransform = usernamePanel.transform;
            }
        }

        void Awake()
        {
            if (transform.GetComponents<RemotePlayer>().Length > 1)
            {
                Destroy(this);
                return;
            }

            animator = GetComponent<Animator>();
            cachedTransform = transform;

            if (animator == null)
            {
                SrLogger.LogWarning("NetworkPlayer has no Animator component!");
            }
        }

        void Start()
        {
            if (usernamePanel)
            {
                usernamePanel.gameObject.AddComponent<TransformLookAtCamera>().targetTransform = usernamePanel.transform;
            }
        }

        public void Update()
        {
            transformTimer -= Time.unscaledDeltaTime;
            if (transformTimer < 0)
            {
                transformTimer = PlayerTimer;

                if (
                    sceneContext == null ||
                    systemContext.SceneLoader == null ||
                    systemContext.SceneLoader.IsSceneLoadInProgress ||
                    sceneContext.GameModel == null ||
                    systemContext.SceneLoader.CurrentSceneGroup == null
                )
                {
                    return;
                }

                if (Main.Client.IsConnected)
                {
                    Main.Client.SendPlayerUpdate(
                        position: cachedTransform.position,
                        rotation: cachedTransform.rotation,
                        horizontalMovement: animator.GetFloat("HorizontalMovement"),
                        forwardMovement: animator.GetFloat("ForwardMovement"),
                        yaw: animator.GetFloat("Yaw"),
                        airborneState: animator.GetInteger("AirborneState"),
                        moving: animator.GetBool("Moving"),
                        horizontalSpeed: animator.GetFloat("HorizontalSpeed"),
                        forwardSpeed: animator.GetFloat("ForwardSpeed"),
                        sprinting: animator.GetBool("Sprinting")
                    );
                }
            }
        }
    }
}