// ReSharper disable UnusedMember.Global
namespace SR2MP.Shared.Utils;

public static class Timers
{   // Time Sync is set to a lower value than 1 to prevent
    public static float WeatherTimer { get; private set; } = 4.5f;
    public static float ActorTimer { get; private set; } = 0.125f;
    public static float PlayerTimer { get; private set; } = 0.125f;
    public static float TimeSyncTimer { get; private set; } = 0.85f;
    public static float PlayerInventoryTimer { get; private set; } = 5f;

    internal enum SyncTimerType : byte
    {
        PLAYER,
        ACTOR,
        PLAYER_INVENTORY,
        WORLD_WEATHER,
        WORLD_TIME
    }

    internal static void SetTimer(SyncTimerType timerType, float value)
    {
        switch (timerType)
        {
            case SyncTimerType.WORLD_WEATHER:
                WeatherTimer = value;
                return;
            case SyncTimerType.ACTOR:
                ActorTimer = value;
                return;
            case SyncTimerType.PLAYER:
                PlayerTimer = value;
                return;
            case SyncTimerType.WORLD_TIME:
                TimeSyncTimer = value;
                return;
            case SyncTimerType.PLAYER_INVENTORY:
                PlayerInventoryTimer = value;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(timerType), timerType, null);
        }
    }
}