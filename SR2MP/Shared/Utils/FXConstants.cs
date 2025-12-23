using System.Collections.ObjectModel;

namespace SR2MP.Shared.Utils;

public static class FXConstants
{
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> IsPlayerSoundDictionary = new(new Dictionary<PlayerFXType, bool>()
    {
        { PlayerFXType.None, false },
        { PlayerFXType.VacReject, false },
        { PlayerFXType.VacAccept, false },
        { PlayerFXType.VacShoot, false },
        
        { PlayerFXType.VacHold, true },
        { PlayerFXType.VacShootEmpty, true },
        { PlayerFXType.VacSlotChange, true },
        { PlayerFXType.VacRunning, true },
        { PlayerFXType.VacRunningStart, true },
        { PlayerFXType.VacRunningEnd, true },
        { PlayerFXType.VacShootSound, true },
    });
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> DoesPlayerSoundLoopDictionary = new(new Dictionary<PlayerFXType, bool>()
    {
        { PlayerFXType.VacHold, false },
        { PlayerFXType.VacShootEmpty, false },
        { PlayerFXType.VacSlotChange, false },
        { PlayerFXType.VacRunningStart, false },
        { PlayerFXType.VacRunningEnd, false },
        { PlayerFXType.VacShootSound, false },
        
        { PlayerFXType.VacRunning, true },
    });
    public static readonly ReadOnlyDictionary<PlayerFXType, float> PlayerSoundVolumeDictionary = new(new Dictionary<PlayerFXType, float>()
    {
        { PlayerFXType.VacShootEmpty, 0.5f },
        { PlayerFXType.VacSlotChange, 0.005f },
        { PlayerFXType.VacRunning, 0.7f },
        { PlayerFXType.VacRunningStart, 0.7f },
        { PlayerFXType.VacRunningEnd, 0.7f },
        { PlayerFXType.VacShootSound, 0.8f },
        { PlayerFXType.VacHold, 0.65f },
    });
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> ShouldPlayerSoundBeTransientDictionary = new(new Dictionary<PlayerFXType, bool>()
    {
        { PlayerFXType.VacRunningStart, false },
        { PlayerFXType.VacRunningEnd, false },
        { PlayerFXType.VacRunning, false },
        
        { PlayerFXType.VacHold, true },
        { PlayerFXType.VacShootEmpty, true },
        { PlayerFXType.VacSlotChange, true },
        { PlayerFXType.VacShootSound, true },
    });
}