using MelonLoader;

namespace SR2MP.Components.FX
{
    // Modified version of PlayerFootstepFX (from a restored decomp of 'PlayerFootstepFX' qwq)
    [RegisterTypeInIl2Cpp(false)]
    public sealed class NetworkPlayerFootstep : MonoBehaviour
    {
        public Transform spawnAtTransform;
        
        public GameObject footstepFX;
        public GameObject footstepFXInstance;
        private ParticleSystem footstepParticles;
        
        private bool playerGrounded;
        private bool playerInWater;

        private float groundCheckDistance = 0.15f;
        
        private void Start()
        {
            spawnAtTransform = transform.GetChild(2);
            footstepFX = fxManager.footstepFX;
            
            footstepFXInstance = Instantiate(footstepFX, spawnAtTransform.position, spawnAtTransform.rotation);
            footstepFXInstance.transform.SetParent(spawnAtTransform.transform);
            
            footstepParticles = footstepFXInstance.GetComponentInChildren<ParticleSystem>();
        }

        public void UpdateFXState()
        {
            if (playerGrounded && !playerInWater)
            {
                footstepParticles.Play(true);
            }
            else
            {
                footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.CompareTag("Water") || collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                playerInWater = true;
                footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("Water") || collider.gameObject.layer == LayerMask.NameToLayer("Water"))
            {
                playerInWater = false;
                
                if (playerGrounded)
                {
                    footstepParticles.Play(true);
                }
            }
        }

        private bool CheckGrounded(int layer)
            => Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, layer);
        
        
        private void Update()
        {   // Don't change it, this is the LayerMask qwq
            // "Magic number that breaks everything if you change it"
            var isGrounded = CheckGrounded(-1728543467);
            
            if (isGrounded != playerGrounded)
            {
                playerGrounded = isGrounded;
                UpdateFXState();
            }
        }
    }
}