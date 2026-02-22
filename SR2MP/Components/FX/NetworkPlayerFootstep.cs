using MelonLoader;

namespace SR2MP.Components.FX;

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

    private const int GroundedLayer = -1728543467;

    public void Awake()
    {
        spawnAtTransform = transform.GetChild(2);
        footstepFX = fxManager.FootstepFX;

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

    public void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;
        playerInWater = true;
        footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void OnTriggerExit(Collider collider)
    {
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;

        playerInWater = false;

        if (playerGrounded)
        {
            footstepParticles.Play(true);
        }
    }

    private bool CheckGrounded(int layer)
        => Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, layer);

    public void Update()
    {   // Don't change it, this is the LayerMask qwq
        // "Magic number that breaks everything if you change it"
        var isGrounded = CheckGrounded(GroundedLayer);

        if (isGrounded == playerGrounded)
            return;
        playerGrounded = isGrounded;
        UpdateFXState();
    }
}