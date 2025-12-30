using SR2E.Utils;
using SR2MP.Components.FX;

namespace SR2MP.Shared.Managers
{
    public class RemoteFXManager
    {
        public readonly Dictionary<string, GameObject> allFX = new();
        public readonly Dictionary<string, SECTR_AudioCue> allCues = new();

        public Dictionary<PlayerFXType, GameObject> playerFXMap = new();
        public Dictionary<PlayerFXType, SECTR_AudioCue> playerAudioCueMap = new();

        public Dictionary<WorldFXType, GameObject> worldFXMap = new();
        public Dictionary<WorldFXType, SECTR_AudioCue> worldAudioCueMap = new();

        public GameObject footstepFX;
        public GameObject sellFX;
        
        internal void Initialize()
        {
            allFX.Clear();
            foreach (var particle in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
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
                { PlayerFXType.VacAccept, allFX["FX_vacAcquire"] },
                { PlayerFXType.VacShoot, allFX["FX_VacpackShoot"] },
                { PlayerFXType.WaterSplash, allFX.ContainsKey("FX_WaterSplash") ? allFX["FX_WaterSplash"] : null! } 
            };
            playerAudioCueMap = new Dictionary<PlayerFXType, SECTR_AudioCue>
            {
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
                { WorldFXType.BuyPlot, allCues["PurchaseRanchTechBase"]},
                { WorldFXType.UpgradePlot, allCues["PurchaseRanchTechUpgrade"]},
                { WorldFXType.SellPlortSound, allCues["SiloReward"]},
                { WorldFXType.SellPlortDroneSound, allCues["SiloRewardDrone"]},
            };
            foreach (var playerFX in playerFXMap)
            {
                if (playerFX.Value)
                {
                    // Please Az find a better way :sob:
                    foreach (var particle in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>()
                                 .Where(x => x.name.Contains(playerFX.Value.name)))
                    {
                        if (!particle.GetComponent<NetworkPlayerFX>())
                            particle.AddComponent<NetworkPlayerFX>().fxType = playerFX.Key;
                    }
                }
            }
            
            foreach (var worldFX in worldFXMap)
            {
                if (worldFX.Value)
                {
                    foreach (var particle in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>()
                                 .Where(x => x.name.Contains(worldFX.Value.name)))
                    {
                        if (!particle.GetComponent<NetworkWorldFX>())
                            particle.AddComponent<NetworkWorldFX>().fxType = worldFX.Key;
                    }
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
        
        public bool TryGetFXType(SECTR_AudioCue cue, out PlayerFXType fxType)
        {
            var gotFx = false;
            fxType = PlayerFXType.None;
        
            foreach (var pair in playerAudioCueMap)
            {
                if (pair.Value == cue)
                {
                    fxType = pair.Key;
                    gotFx = true;
                
                    break;
                }
            }
        
            return gotFx;
        }
        public bool TryGetFXType(SECTR_AudioCue cue, out WorldFXType fxType)
        {
            var gotFx = false;
            fxType = WorldFXType.None;
        
            foreach (var pair in worldAudioCueMap)
            {
                if (pair.Value == cue)
                {
                    fxType = pair.Key;
                    gotFx = true;
                
                    break;
                }
            }
        
            return gotFx;
        }
        
        public void PlayTransientAudio(SECTR_AudioCue cue, Vector3 position, float volume, bool loop = false)
        {
            var played = SECTR_AudioSystem.Play(cue, position, loop);

            played.Volume = volume;
        }
    }
}