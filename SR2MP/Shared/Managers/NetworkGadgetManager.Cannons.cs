namespace SR2MP.Shared.Managers;

internal static partial class NetworkGadgetManager
{
    private static readonly Dictionary<long, double> CannonFireTimes = new();

    /// <summary>
    /// Records a cannon's next fire time
    /// </summary>
    /// <remarks>
    /// A cannon first seen is taken silently,
    /// broadcasting it would have every player
    /// announce their own timer the moment the gadget loads.
    /// </remarks>
    internal static bool TryRecordCannonFireTime(long gadgetId, double nextFireTime)
    {
        if (!CannonFireTimes.TryGetValue(gadgetId, out var previous))
        {
            CannonFireTimes[gadgetId] = nextFireTime;
            return false;
        }
        
        // no ammo = NaN, would broadcast on every frame
        if (previous.Equals(nextFireTime) || Math.Abs(previous - nextFireTime) < 0.0001)
            return false;

        CannonFireTimes[gadgetId] = nextFireTime;
        return true;
    }

    // Whoever fires first pushes everyone else's timer past the trigger
    internal static void ApplyCannonFireTime(long gadgetId, double nextFireTime)
    {
        CannonFireTimes[gadgetId] = nextFireTime;

        if (!TryGetGadgetModel(gadgetId, out var model) || model == null)
            return;

        var gameObject = model.GetGameObject();
        if (!gameObject)
            return;

        var cannon = gameObject.GetComponentInChildren<LinkedCannonInput>(true);
        if (!cannon)
            return;

        cannon._nextFireTime = nextFireTime;

        PlayCannonFireEffects(cannon);
    }
    
    private static LinkedCannonOutput? ResolveCannonOutput(LinkedCannonInput cannon)
    {
        if (cannon._linkedCannonOutput)
            return cannon._linkedCannonOutput;

        var linked = cannon._model?.GetLinkedGadget();
        var linkedObject = linked?.GetGameObject();

        if (!linkedObject)
            return null;

        return cannon._linkedCannonOutput = linkedObject!.GetComponentInChildren<LinkedCannonOutput>(true);
    }
    
    private static void PlayCannonFireEffects(LinkedCannonInput cannon)
    {
        try
        {
            var output = ResolveCannonOutput(cannon);

            if (!output)
                return;

            if (output!._cannonFireSpring)
                output._cannonFireSpring.Nudge(output._cannonFireSpringNudgeAmount);

            if (output._onFireCue)
                SECTR_AudioSystem.Play(output._onFireCue, output.transform.position, false);

            if (output._onFireFX && output._ejectPoint)
                FXHelpers.SpawnAndPlayFX(output._onFireFX, output._ejectPoint.position, output._ejectPoint.rotation);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"NetworkGadgetManager: cannon fire effects failed: {ex.Message}");
        }
    }
}
