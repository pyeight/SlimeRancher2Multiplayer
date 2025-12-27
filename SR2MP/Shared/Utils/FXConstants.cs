using System.Collections.ObjectModel;

namespace SR2MP.Shared.Utils;

public static class FXConstants
{
    #region Player FX
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> IsPlayerSoundDictionary = new(new Dictionary<PlayerFXType, bool>
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
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> DoesPlayerSoundLoopDictionary = new(new Dictionary<PlayerFXType, bool>
    {
        { PlayerFXType.VacHold, false },
        { PlayerFXType.VacShootEmpty, false },
        { PlayerFXType.VacSlotChange, false },
        { PlayerFXType.VacRunningStart, false },
        { PlayerFXType.VacRunningEnd, false },
        { PlayerFXType.VacShootSound, false },

        { PlayerFXType.VacRunning, true },
    });
    public static readonly ReadOnlyDictionary<PlayerFXType, float> PlayerSoundVolumeDictionary = new(new Dictionary<PlayerFXType, float>
    {
        { PlayerFXType.VacShootEmpty, 0.5f },
        { PlayerFXType.VacSlotChange, 0.005f },
        { PlayerFXType.VacRunning, 0.7f },
        { PlayerFXType.VacRunningStart, 0.7f },
        { PlayerFXType.VacRunningEnd, 0.7f },
        { PlayerFXType.VacShootSound, 0.8f },
        { PlayerFXType.VacHold, 0.65f },
    });
    public static readonly ReadOnlyDictionary<PlayerFXType, bool> ShouldPlayerSoundBeTransientDictionary = new(new Dictionary<PlayerFXType, bool>
    {
        { PlayerFXType.VacRunningStart, false },
        { PlayerFXType.VacRunningEnd, false },
        { PlayerFXType.VacRunning, false },

        { PlayerFXType.VacHold, true },
        { PlayerFXType.VacShootEmpty, true },
        { PlayerFXType.VacSlotChange, true },
        { PlayerFXType.VacShootSound, true },
    });

    #endregion

    #region World FX
    public static readonly ReadOnlyDictionary<WorldFXType, bool> IsWorldSoundDictionary = new(new Dictionary<WorldFXType, bool>
    {
        { WorldFXType.None, false },
        { WorldFXType.SellPlort, false },

        { WorldFXType.BuyPlot, true },
        { WorldFXType.UpgradePlot, true },
        { WorldFXType.SellPlortSound, true },
        { WorldFXType.SellPlortDroneSound, true },
    });
    public static readonly ReadOnlyDictionary<WorldFXType, bool> DoesWorldSoundLoopDictionary = new(new Dictionary<WorldFXType, bool>
    {
        { WorldFXType.BuyPlot, false },
        { WorldFXType.UpgradePlot, false },
        { WorldFXType.SellPlortSound, false },
        { WorldFXType.SellPlortDroneSound, false },
    });
    public static readonly ReadOnlyDictionary<WorldFXType, float> WorldSoundVolumeDictionary = new(new Dictionary<WorldFXType, float>
    {
        { WorldFXType.BuyPlot, 0.5f },
        { WorldFXType.UpgradePlot, 0.5f },
        { WorldFXType.SellPlortSound, 0.5f },
        { WorldFXType.SellPlortDroneSound, 0.5f },
    });
    public static readonly ReadOnlyDictionary<WorldFXType, bool> ShouldWorldSoundBeTransientDictionary = new(new Dictionary<WorldFXType, bool>
    {
        { WorldFXType.BuyPlot, true },
        { WorldFXType.UpgradePlot, true },
        { WorldFXType.SellPlortSound, true },
        { WorldFXType.SellPlortDroneSound, true },
    });

    #endregion
}