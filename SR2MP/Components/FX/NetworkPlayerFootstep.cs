using JetBrains.Annotations;
using MelonLoader;
using Starlight.Storage;

namespace SR2MP.Components.FX;

// Modified version of PlayerFootstepFX (from a restored decomp of 'PlayerFootstepFX' qwq)
[InjectIntoIL]
internal sealed class NetworkPlayerFootstep : MonoBehaviour
{
    public Transform SpawnAtTransform;

    public GameObject FootstepFX;
    public GameObject FootstepFXInstance;
    private ParticleSystem footstepParticles;

    private bool playerGrounded;
    private bool playerInWater;

    private const float GroundCheckDistance = 0.15f;
    private const int GroundedLayer = -1728543467;

    [UsedImplicitly]
    public void Awake()
    {
        SpawnAtTransform = transform.GetChild(2);
        FootstepFX = FXManager.FootstepFX;

        FootstepFXInstance = Instantiate(FootstepFX, SpawnAtTransform.position, SpawnAtTransform.rotation);
        FootstepFXInstance.transform.SetParent(SpawnAtTransform.transform);

        footstepParticles = FootstepFXInstance.GetComponentInChildren<ParticleSystem>();
    }

    public void UpdateFXState()
    {
        if (playerGrounded && !playerInWater)
            footstepParticles.Play(true);
        else
            footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [UsedImplicitly]
    public void OnTriggerEnter(Collider collider)
    {
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;

        playerInWater = true;
        footstepParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [UsedImplicitly]
    public void OnTriggerExit(Collider collider)
    {
        if (!collider.CompareTag("Water") && collider.gameObject.layer != LayerMask.NameToLayer("Water"))
            return;

        playerInWater = false;

        if (playerGrounded)
            footstepParticles.Play(true);
    }

    private bool CheckGrounded(int layer)
        => Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance, layer);

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