using Il2CppMonomiPark.SlimeRancher.Drone;

namespace SR2MP.Components.Drone;

internal sealed partial class NetworkDrone
{
    internal struct AnimatorParameter
    {
        public int Hash;
        public byte Type;
        public float Value;
    }
    
    private int lastAnimatorState;
    private int pendingAnimatorState;

    private List<(int Hash, AnimatorControllerParameterType Type)>? cachedParameters;

    private List<AnimatorParameter> GetAnimatorParameters()
    {
        var result = new List<AnimatorParameter>();

        var animator = GetDroneAnimator()?._animator;
        if (animator?.isActiveAndEnabled != true)
            return result;

        if (cachedParameters == null)
        {
            cachedParameters = new List<(int, AnimatorControllerParameterType)>();

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type is AnimatorControllerParameterType.Float
                    or AnimatorControllerParameterType.Bool
                    or AnimatorControllerParameterType.Int)
                    cachedParameters.Add((parameter.nameHash, parameter.type));
            }
        }

        foreach (var (hash, type) in cachedParameters)
        {
            var value = type switch
            {
                AnimatorControllerParameterType.Float => animator.GetFloat(hash),
                AnimatorControllerParameterType.Bool => animator.GetBool(hash) ? 1f : 0f,
                _ => animator.GetInteger(hash)
            };

            result.Add(new AnimatorParameter
            {
                Hash = hash,
                Type = (byte)type,
                Value = value
            });
        }

        return result;
    }

    private static int GetParameterHash(List<AnimatorParameter> animParams)
    {
        var hash = 17;

        foreach (var param in animParams)
            hash = hash * 31 + param.Hash + (int)(param.Value * 100f);

        return hash;
    }

    private int GetCurrentAnimatorState()
    {
        var animator = GetDroneAnimator()?._animator;
        if (animator?.isActiveAndEnabled != true)
            return 0;

        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
    }

    private QuantumDroneAnimator? GetDroneAnimator()
    {
        if (ranchDrone?._droneAnimator != null)
            return ranchDrone._droneAnimator;

        return GetComponentInChildren<QuantumDroneAnimator>(true);
    }

    private void ApplyAnimatorParameters(List<AnimatorParameter> parameters)
    {
        if (parameters.Count == 0)
            return;

        var animator = GetDroneAnimator()?._animator;
        if (animator?.isActiveAndEnabled != true)
            return;

        foreach (var param in parameters)
        {
            switch ((AnimatorControllerParameterType)param.Type)
            {
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.Hash, param.Value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(param.Hash, param.Value >= 0.5f);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.Hash, (int)param.Value);
                    break;
            }
        }
    }

    private void ApplyAnimatorState(int animState)
    {
        if (animState == 0 || animState == lastAnimatorState)
        {
            pendingAnimatorState = 0;
            return;
        }

        if (animState != pendingAnimatorState)
        {
            pendingAnimatorState = animState;
            return;
        }

        pendingAnimatorState = 0;

        var animator = GetDroneAnimator()?._animator;
        if (animator?.isActiveAndEnabled != true)
            return;

        if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == animState)
        {
            lastAnimatorState = animState;
            return;
        }

        try
        {
            animator.CrossFade(animState, 0.1f, 0);
        }
        catch (Exception ex)
        {
            SrLogger.LogDebug($"Failed to apply drone anim state {animState}: {ex.Message}");
        }

        lastAnimatorState = animState;
    }
}
