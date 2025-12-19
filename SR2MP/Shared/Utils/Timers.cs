namespace SR2MP.Shared.Utils;

public static class Timers
{
    private static float timeSyncTimer = 1f;
    private static float actorSyncTimer = 0.275f;
    private static float playerSyncTimer = 0.275f;
    private static float weatherSyncTimer = 1.0f;
    private static float playerInventorySyncTimer = 5.0f;
    private static float planterSyncTimer = 2.5f;

    // not sure if we should use those
    private static float actorLoadRadius = 200f;
    private static float actorUnloadRadius = 250f;

    public static float WeatherTimer => weatherSyncTimer;
    public static float ActorTimer => actorSyncTimer;
    public static float PlayerTimer => playerSyncTimer;
    public static float TimeSyncTimer => timeSyncTimer;
    public static float PlayerInventoryTimer => playerInventorySyncTimer;
    public static float PlanterTimer => planterSyncTimer;

    public static float ActorLoadRadius => actorLoadRadius;
    public static float ActorUnloadRadius => actorUnloadRadius;

    public enum SyncTimerType
    {
        PLAYER,
        ACTOR,
        PLAYER_INVENTORY,
        WORLD_WEATHER,
        WORLD_TIME,
        WORLD_PLANTER,
    }

    internal static void SetTimer(SyncTimerType timerType, float value)
    {
        switch (timerType)
        {
            case SyncTimerType.WORLD_WEATHER:
                weatherSyncTimer = value;
                return;
            case SyncTimerType.ACTOR:
                actorSyncTimer = value;
                return;
            case SyncTimerType.PLAYER:
                playerSyncTimer = value;
                return;
            case SyncTimerType.WORLD_TIME:
                timeSyncTimer = value;
                return;
            case SyncTimerType.PLAYER_INVENTORY:
                playerInventorySyncTimer = value;
                return;
            case SyncTimerType.WORLD_PLANTER:
                planterSyncTimer = value;
                return;
        }
    }
}