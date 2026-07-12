using SR2MP.Components.Actor;

namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    private const float ForceFieldSearchRadius = 5f;
    private const float ForceFieldSlimeRadius = 4f;
    private const float ForceFieldOtherRadius = 1.2f;
    private const float ForceStrength = 10f;

    /*private void UpdateForceField()
    {
        if (IsLocal) return;

        var position = transform.position;
        var hits = Physics.OverlapSphere(position, ForceFieldSearchRadius);

        foreach (var col in hits)
        {
            if (!col) continue;

            var rb = col.attachedRigidbody;
            if (!rb || rb.isKinematic) continue;

            var actor = col.GetComponent<NetworkActor>();
            if (!actor || !actor.LocallyOwned) continue;

            var offset = col.transform.position - position;
            var distance = offset.magnitude;
            if (distance < 0.001f) continue;

            var radius = actor.isSlime ? ForceFieldSlimeRadius : ForceFieldOtherRadius;
            var falloff = Mathf.Clamp01(1f - Mathf.Pow(distance / radius, ForceFalloff));
            var force = (offset.normalized + Vector3.up * 0.3f)
                        * (ForceStrength * falloff);

            rb.AddForce(force, ForceMode.Force);
        }
    }*/
    
    private void UpdateForceField()
    {
        if (IsLocal) return;

        var position = transform.position;

        var hits = Physics.OverlapSphere(position, ForceFieldSearchRadius);

        foreach (var col in hits)
        {
            if (!col) continue;

            var rb = col.attachedRigidbody;
            if (!rb || rb.isKinematic) continue;

            var actor = col.GetComponent<NetworkActor>();
            if (!actor || !actor.LocallyOwned) continue;

            var offset = col.transform.position - position;
            var distance = offset.magnitude;
            if (distance < 0.001f) continue;

            var radius = actor.isSlime ? ForceFieldSlimeRadius : ForceFieldOtherRadius;

            var dir = offset / distance;
            
            var t = Mathf.Clamp01(distance / radius);
            
            var smooth = 1f - (t * t * (3f - (2f * t)));
            
            var boost = Mathf.Clamp01(1f - (t * 2f));

            var force = (dir + (Vector3.up * 0.15f)).normalized * (ForceStrength * (smooth + (boost * 1.5f)));
            
            rb.AddForce(force, ForceMode.Acceleration);
            
            var velocity = rb.linearVelocity;

            var radialVel = Vector3.Dot(velocity, dir) * dir;
            var tangentialVel = velocity - radialVel;
            
            rb.AddForce(-tangentialVel * 0.4f, ForceMode.Force);
            
            if (t < 0.25f)
                rb.AddForce(dir * (ForceStrength * 1.5f), ForceMode.Force);
        }
    }
}