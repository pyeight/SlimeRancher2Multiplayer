using SR2E.Utils;
using SR2MP.Components.FX;

namespace SR2MP.Shared.Managers;

public sealed class RemoteFXManager
{
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly Dictionary<string, GameObject> AllFX = new();
    public readonly Dictionary<string, SECTR_AudioCue> AllCues = new();

    public Dictionary<PlayerFXType, GameObject> PlayerFXMap;
    public Dictionary<PlayerFXType, SECTR_AudioCue> PlayerAudioCueMap;

    public Dictionary<WorldFXType, GameObject> WorldFXMap;
    public Dictionary<WorldFXType, SECTR_AudioCue> WorldAudioCueMap;

    public GameObject FootstepFX;
    public GameObject? SellFX;

    private static Predicate<SECTR_AudioCue> Force3DCondition => cue =>
    {
        // Movement SFX
        if (cue.name.Contains("Step")
            || cue.name.Contains("Run")
            || cue.name.Contains("Jump")
            || cue.name.Contains("Land"))
        {
            return true;
        }
        
        // VAC SFX
        if (cue.name.Contains("Vac")
            || cue.name.Contains("vac"))
        {
            return true;
        }

        return false;
    };

    internal void Initialize()
    {
        AllFX.Clear();
        var resources = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
        foreach (var particle in resources)
        {
            var particleName = particle.gameObject.name.Replace(' ', '_');

            AllFX.TryAdd(particleName, particle.gameObject);
        }
        AllCues.Clear();
        foreach (var cue in Resources.FindObjectsOfTypeAll<SECTR_AudioCue>())
        {
            if (cue.Spatialization != SECTR_AudioCue.Spatializations.Simple2D)
                cue.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;

            if (Force3DCondition(cue))
                cue.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;

            var cueName = cue.name.Replace(' ', '_');
            AllCues.TryAdd(cueName, cue);
        }
        PlayerFXMap = new Dictionary<PlayerFXType, GameObject>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacReject, AllFX["FX_vacReject"] },
            { PlayerFXType.VacAccept, AllFX["FX_vacAcquire"] },
            { PlayerFXType.VacShoot, AllFX["FX_VacpackShoot"] }
        };
        PlayerAudioCueMap = new Dictionary<PlayerFXType, SECTR_AudioCue>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacShootEmpty, AllCues["VacShootEmpty"]},
            { PlayerFXType.VacHold, AllCues["VacClogged"]},
            { PlayerFXType.VacSlotChange, AllCues["VacAmmoSelect"]},
            { PlayerFXType.VacRunning, AllCues["VacRun"]},
            { PlayerFXType.VacRunningStart, AllCues["VacStart"]},
            { PlayerFXType.VacRunningEnd, AllCues["VacEnd"]},
            { PlayerFXType.VacShootSound, AllCues["VacShoot"]},
        };
        WorldFXMap = new Dictionary<WorldFXType, GameObject>
        {
            { WorldFXType.None, null! },
            { WorldFXType.SellPlort, SellFX ?? AllFX["FX_Stars"] },
            { WorldFXType.FavoriteFoodEaten, AllFX["FX_slimeEatFav"] },
            { WorldFXType.GordoFoodEaten, AllFX["FX_Gordo_Eat"] },
        };
        WorldAudioCueMap = new Dictionary<WorldFXType, SECTR_AudioCue>
        {
            { WorldFXType.None, null! },
            { WorldFXType.BuyPlot, AllCues["PurchaseRanchTechBase"]},
            { WorldFXType.UpgradePlot, AllCues["PurchaseRanchTechUpgrade"]},
            { WorldFXType.SellPlortSound, AllCues["SiloReward"]},
            { WorldFXType.SellPlortDroneSound, AllCues["SiloRewardDrone"]},
            { WorldFXType.GordoFoodEatenSound, AllCues["GordoGulp"] },
        };

        foreach (var (playerFX, obj) in PlayerFXMap)
        {
            if (!obj)
                continue;

            // Please Az find a better way :sob:
            // Made slight improvements - Az
            foreach (var particle in resources.Where(x => x.name.Contains(obj.name)))
            {
                if (!particle.GetComponent<NetworkPlayerFX>())
                    particle.AddComponent<NetworkPlayerFX>().fxType = playerFX;
            }
        }

        foreach (var (worldFX, obj) in WorldFXMap)
        {
            if (!obj)
                continue;

            foreach (var particle in resources.Where(x => x.name.Contains(obj.name)))
            {
                if (!particle.GetComponent<NetworkWorldFX>())
                    particle.AddComponent<NetworkWorldFX>().fxType = worldFX;
            }
        }

        FootstepFX = AllFX["FX_Footstep"];

        foreach (var cue in PlayerAudioCueMap)
        {
            if (cue.Value)
                cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }
        foreach (var cue in WorldAudioCueMap)
        {
            if (cue.Value)
                cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }

        SrLogger.LogMessage("RemoteFXManager initialized", SrLogTarget.Both);
    }

    public bool TryGetFXType(SECTR_AudioCue cue, out PlayerFXType fxType) => TryGetFXType(cue, PlayerAudioCueMap, out fxType);

    public bool TryGetFXType(SECTR_AudioCue cue, out WorldFXType fxType) => TryGetFXType(cue, WorldAudioCueMap, out fxType);

    private static bool TryGetFXType<T>(SECTR_AudioCue cue, Dictionary<T, SECTR_AudioCue> cueMap, out T fxType) where T : struct, Enum
    {
        fxType = default;

        if (cueMap == null)
            return false;

        foreach (var pair in cueMap)
        {
            if (pair.Value != cue)
                continue;

            fxType = pair.Key;
            return true;
        }

        return false;
    }

    public static void PlayTransientAudio(SECTR_AudioCue cue, Vector3 position, bool loop = false)
    {
        SECTR_AudioSystem.Play(cue, position, loop);
    }

    public static void PlayTransientAudio(SECTR_AudioCue cue, Vector3 position, float volume, bool loop = false)
    {
        var played = SECTR_AudioSystem.Play(cue, position, loop);

        played.Volume = volume;
    }
}