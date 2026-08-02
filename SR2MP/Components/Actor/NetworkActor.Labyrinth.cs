using Il2CppMonomiPark.SlimeRancher.Labyrinth;

namespace SR2MP.Components.Actor;

internal sealed partial class NetworkActor
{
    private DisruptableResource? disruptableResource;
    private UnstableResource? unstableResource;

    private bool vanillaDisruptableEnabled;
    private bool vanillaUnstableEnabled;

    private bool isLabyrinthResource;

    private void InitializeLabyrinthComponents()
    {
        disruptableResource = GetComponent<DisruptableResource>();
        unstableResource = GetComponent<UnstableResource>();

        if (disruptableResource != null)
            vanillaDisruptableEnabled = disruptableResource.enabled;

        if (unstableResource != null)
            vanillaUnstableEnabled = unstableResource.enabled;

        isLabyrinthResource = disruptableResource != null || unstableResource != null;

        // if (isLabyrinthResource)
        //    SrLogger.LogDebug($"Labyrinth resource {ActorId.Value}: disruptable={disruptableResource != null}, unstable={unstableResource != null}, locallyOwned={LocallyOwned}");
    }
    
    private void ApplyLabyrinthSimulationGate()
    {
        if (!isLabyrinthResource)
            return;

        if (disruptableResource != null)
            disruptableResource.enabled = LocallyOwned && vanillaDisruptableEnabled;

        if (unstableResource != null)
            unstableResource.enabled = LocallyOwned && vanillaUnstableEnabled;
    }

    private void ResetLabyrinthToVanilla()
    {
        if (!isLabyrinthResource)
            return;

        if (disruptableResource != null)
            disruptableResource.enabled = vanillaDisruptableEnabled;

        if (unstableResource != null)
            unstableResource.enabled = vanillaUnstableEnabled;
    }
}