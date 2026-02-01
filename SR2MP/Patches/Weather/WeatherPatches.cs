using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.Weather
{
    // Weather Registry
    
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.Awake))]
    public static class WeatherRegistryAwakePatch
    {
        public static void Postfix(WeatherRegistry __instance)
        {
            WeatherUpdateHelper.CreateWeatherPatternLookup(__instance);
        }
    }
    
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.Update))]
    public static class WeatherRegistryUpdatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected)
            {
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.RunPatternState))]
    public static class WeatherRegistryRunPatternStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogPacketSize("Blocked weather registry RunPatternState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogPacketSize("Allowed weather registry RunPatternState", SrLogTarget.Both);
            return true;
        }
    }

    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.StopPatternState))]
    public static class WeatherRegistryStopPatternStatePatch
    {
        public static bool Prefix(WeatherRegistry __instance, 
            ZoneDefinition zone,
            IWeatherPattern pattern,
            IWeatherState state)
        {
            if (__instance == null)
            {
                SrLogger.LogError("StopPatternState: WeatherRegistry instance is null", SrLogTarget.Both);
                return false;
            }

            if (zone == null)
            {
                SrLogger.LogError("StopPatternState: Zone is null", SrLogTarget.Both);
                return false;
            }

            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogPacketSize("Blocked weather registry StopPatternState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogPacketSize($"Allowed weather registry StopPatternState (Zone: {zone.name}, Pattern: {pattern}, State: {state})", SrLogTarget.Both);
            return true;
        }
    }
    
    // Weather Director
    
    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.FixedUpdate))]
    public static class WeatherDirectorFixedUpdatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected)
                return false;

            return true;
        }

        public static void Postfix()
        {
            if (Main.Server.IsRunning())
            {
                WeatherUpdateHelper.Update();
            }
        }
    }

    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.RunState))]
    public static class WeatherDirectorRunStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogPacketSize("Blocked client weather RunState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogPacketSize("Allowed client weather RunState", SrLogTarget.Both);
            return true;
        }

        public static void Postfix()
        {
            if (Main.Server.IsRunning() && !handlingPacket)
            {
                WeatherUpdateHelper.SendWeatherUpdate();
            }
        }
    }

    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.StopState))]
    public static class WeatherDirectorStopStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogPacketSize("Blocked weather StopState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogPacketSize("Allowed weather StopState", SrLogTarget.Both);
            return true;
        }

        public static void Postfix()
        {
            if (Main.Server.IsRunning() && !handlingPacket)
            {
                WeatherUpdateHelper.SendWeatherUpdate();
            }
        }
    }
}

public static class WeatherUpdateHelper
{
    private static bool initializedWeatherPatternLookup = false;

    private static float timeSinceLastUpdate;
    private const float UpdateInterval = 1.0f;

    public static void Update()
    {
        if (!Main.Server.IsRunning())
            return;

        timeSinceLastUpdate += Time.fixedDeltaTime;

        if (timeSinceLastUpdate >= UpdateInterval)
        {
            timeSinceLastUpdate = 0f;
            SendWeatherUpdate();
        }
    }

    public static void CreateWeatherPatternLookup(WeatherRegistry registry)
    {
        if (initializedWeatherPatternLookup)
        {
            SrLogger.LogWarning("Weather pattern lookup already initialized, skipping", SrLogTarget.Both);
            return;
        }

        if (registry == null)
        {
            SrLogger.LogError("Cannot create weather pattern lookup: registry is null", SrLogTarget.Both);
            return;
        }
        
        weatherPatternsFromStateNames.Clear();
        weatherPatternsByZone.Clear();

        int totalPatterns = 0;
        int totalStates = 0;
        int nullStates = 0;
        int duplicateStates = 0;
        int totalZones = 0;

        foreach (var config in registry.ZoneConfigList)
        {
            totalZones++;
            SrLogger.LogPacketSize($"Processing zone: {config.Zone.name} with {config.Patterns.Count} patterns", SrLogTarget.Both);
            
            var zonePatternMap = new Dictionary<string, WeatherPatternDefinition>();

            foreach (var pattern in config.Patterns)
            {
                totalPatterns++;

                foreach (var state in pattern._stateList)
                {
                    totalStates++;

                    zonePatternMap[state.name] = pattern;
                    
                    if (!weatherPatternsFromStateNames.TryAdd(state.name, pattern))
                        duplicateStates++;
                }
            }
            
            weatherPatternsByZone[config.Zone] = zonePatternMap;
            SrLogger.LogPacketSize($"Zone {config.Zone.name}: Mapped {zonePatternMap.Count} states to patterns", SrLogTarget.Both);
        }
        
        SrLogger.LogMessage($"Initialized WeatherPatternLookup with {totalPatterns} patterns across {totalZones} zones", SrLogTarget.Both);
        SrLogger.LogMessage($"{totalStates} weather states ({duplicateStates} duplicate, {nullStates} null)", SrLogTarget.Both);

        initializedWeatherPatternLookup = true;
    }
    
    public static WeatherPatternDefinition GetPatternForZoneAndState(ZoneDefinition zone, string stateName)
    {
        if (weatherPatternsByZone.TryGetValue(zone, out var zoneMap))
        {
            if (zoneMap.TryGetValue(stateName, out var pattern))
            {
                return pattern;
            }
        }
        
        if (weatherPatternsFromStateNames.TryGetValue(stateName, out var fallbackPattern))
        {
            SrLogger.LogWarning($"Using fallback pattern for {zone.name} / {stateName}: {fallbackPattern.name}", SrLogTarget.Both);
            return fallbackPattern;
        }

        return null!;
    }
    
    public static void SendWeatherUpdate()
    {
        if (!Main.Server.IsRunning())
            return;

        var weatherRegistry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();

        MelonCoroutines.Start(
            WeatherPacket.CreateFromModel(
                weatherRegistry!._model,
                PacketType.WeatherUpdate,
                packet => Main.Server.SendToAll(packet)
            )
        );
    }
}