using SR2E.Utils;
using SR2MP.Components.FX;

namespace SR2MP.Shared.Managers;

public sealed class RemoteFXManager
{
    public readonly Dictionary<string, GameObject> allFX = new();
    public readonly Dictionary<string, SECTR_AudioCue> allCues = new();

    public Dictionary<PlayerFXType, GameObject> playerFXMap;
    public Dictionary<PlayerFXType, SECTR_AudioCue> playerAudioCueMap;

    public Dictionary<WorldFXType, GameObject> worldFXMap;
    public Dictionary<WorldFXType, SECTR_AudioCue> worldAudioCueMap;

    public GameObject footstepFX;
    public GameObject? sellFX;

    internal void Initialize()
    {
        allFX.Clear();
        var resources = Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>();
        foreach (var particle in resources)
        {
            var particleName = particle.gameObject.name.Replace(' ', '_');

            allFX.TryAdd(particleName, particle.gameObject);
        }
        allCues.Clear();
        foreach (var cue in Resources.FindObjectsOfTypeAll<SECTR_AudioCue>())
        {
            if (cue.Spatialization != SECTR_AudioCue.Spatializations.Simple2D)
                cue.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;

            var cueName = cue.name.Replace(' ', '_');
            allCues.TryAdd(cueName, cue);
        }
        playerFXMap = new Dictionary<PlayerFXType, GameObject>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacReject, allFX["FX_vacReject"] },
            { PlayerFXType.VacAccept, allFX["FX_vacAcquire"] },
            { PlayerFXType.VacShoot, allFX["FX_VacpackShoot"] }
        };
        playerAudioCueMap = new Dictionary<PlayerFXType, SECTR_AudioCue>
        {
            { PlayerFXType.None, null! },
            { PlayerFXType.VacShootEmpty, allCues["VacShootEmpty"]},
            { PlayerFXType.VacHold, allCues["VacClogged"]},
            { PlayerFXType.VacSlotChange, allCues["VacAmmoSelect"]},
            { PlayerFXType.VacRunning, allCues["VacRun"]},
            { PlayerFXType.VacRunningStart, allCues["VacStart"]},
            { PlayerFXType.VacRunningEnd, allCues["VacEnd"]},
            { PlayerFXType.VacShootSound, allCues["VacShoot"]},
        };
        worldFXMap = new Dictionary<WorldFXType, GameObject>
        {
            { WorldFXType.None, null! },
            { WorldFXType.SellPlort, sellFX ?? allFX["FX_Stars"] },
        };
        worldAudioCueMap = new Dictionary<WorldFXType, SECTR_AudioCue>
        {
            { WorldFXType.None, null! },
            { WorldFXType.BuyPlot, allCues["PurchaseRanchTechBase"]},
            { WorldFXType.UpgradePlot, allCues["PurchaseRanchTechUpgrade"]},
            { WorldFXType.SellPlortSound, allCues["SiloReward"]},
            { WorldFXType.SellPlortDroneSound, allCues["SiloRewardDrone"]},
        };

        foreach (var (playerFX, obj) in playerFXMap)
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

        foreach (var (worldFX, obj) in worldFXMap)
        {
            if (!obj)
                continue;

            foreach (var particle in resources.Where(x => x.name.Contains(obj.name)))
            {
                if (!particle.GetComponent<NetworkWorldFX>())
                    particle.AddComponent<NetworkWorldFX>().fxType = worldFX;
            }
        }

        footstepFX = allFX["FX_Footstep"];

        foreach (var cue in playerAudioCueMap)
        {
            cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }
        foreach (var cue in worldAudioCueMap)
        {
            cue.Value.Spatialization = SECTR_AudioCue.Spatializations.Occludable3D;
        }

        SrLogger.LogMessage("RemoteFXManager initialized", SrLogger.LogTarget.Both);
    }

    public bool TryGetFXType(SECTR_AudioCue cue, out PlayerFXType fxType) => TryGetFXType(cue, playerAudioCueMap, out fxType);

    public bool TryGetFXType(SECTR_AudioCue cue, out WorldFXType fxType) => TryGetFXType(cue, worldAudioCueMap, out fxType);

    private static bool TryGetFXType<T>(SECTR_AudioCue cue, Dictionary<T, SECTR_AudioCue> cueMap, out T fxType) where T : struct, Enum
    {
        fxType = default;

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
}