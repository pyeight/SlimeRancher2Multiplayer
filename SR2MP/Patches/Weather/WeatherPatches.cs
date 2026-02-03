using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using Il2CppMonomiPark.SlimeRancher.World;

namespace SR2MP.Patches.Weather
{
    // Weather Registry

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
            WeatherUpdateHelper.EnsureLookupInitialized();

            if (Main.Client.IsConnected && !handlingPacket)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.StopPatternState))]
    public static class WeatherRegistryStopPatternStatePatch
    {
        public static bool Prefix(
            WeatherRegistry __instance,
            ZoneDefinition zone,
            IWeatherPattern pattern,
            IWeatherState state)
        {
            WeatherUpdateHelper.EnsureLookupInitialized();

            if (__instance == null || zone == null)
                return false;

            if (Main.Client.IsConnected && !handlingPacket)
            {
                return false;
            }

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
    }

    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.RunState))]
    public static class WeatherDirectorRunStatePatch
    {
        public static bool Prefix()
        {
            WeatherUpdateHelper.EnsureLookupInitialized();

            if (Main.Client.IsConnected && !handlingPacket)
            {
                return false;
            }

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
            WeatherUpdateHelper.EnsureLookupInitialized();

            if (Main.Client.IsConnected && !handlingPacket)
            {
                return false;
            }

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
    private static bool lookupInitialized = false;
    private static readonly Dictionary<string, WeatherPatternDefinition> weatherPatternsFromStateNames = new();
    private static readonly Dictionary<ZoneDefinition, Dictionary<string, WeatherPatternDefinition>> weatherPatternsByZone = new();

    public static void CreateWeatherPatternLookup(WeatherRegistry registry)
    {
        if (registry == null)
        {
            SrLogger.LogError("WeatherRegistry is null in CreateWeatherPatternLookup", SrLogTarget.Both);
            return;
        }

        try
        {
            weatherPatternsFromStateNames.Clear();
            weatherPatternsByZone.Clear();

            if (registry.ZoneConfigList == null)
            {
                SrLogger.LogError("WeatherRegistry.ZoneConfigList is null", SrLogTarget.Both);
                return;
            }

            foreach (var config in registry.ZoneConfigList)
            {
                if (config == null || config.Zone == null || config.Patterns == null)
                {
                    SrLogger.LogPacketSize("Skipping null weather config or patterns", SrLogTarget.Both);
                    continue;
                }

                var zonePatternMap = new Dictionary<string, WeatherPatternDefinition>();

                foreach (var pattern in config.Patterns)
                {
                    if (pattern == null || pattern._stateList == null)
                    {
                        SrLogger.LogPacketSize($"Skipping null pattern or state list in zone {config.Zone.name}", SrLogTarget.Both);
                        continue;
                    }

                    foreach (var state in pattern._stateList)
                    {
                        if (state == null || string.IsNullOrEmpty(state.name))
                        {
                            SrLogger.LogPacketSize($"Skipping null state or state name in pattern {pattern.name}", SrLogTarget.Both);
                            continue;
                        }

                        zonePatternMap[state.name] = pattern;
                        weatherPatternsFromStateNames.TryAdd(state.name, pattern);
                    }
                }

                weatherPatternsByZone[config.Zone] = zonePatternMap;
            }

            lookupInitialized = true;
            SrLogger.LogPacketSize($"Weather pattern lookup initialized with {weatherPatternsFromStateNames.Count} states", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error in CreateWeatherPatternLookup: {ex}", SrLogTarget.Both);
            lookupInitialized = false;
        }
    }

    public static void EnsureLookupInitialized()
    {
        if (lookupInitialized)
            return;

        try
        {
            var registry = Resources
                .FindObjectsOfTypeAll<WeatherRegistry>()
                .FirstOrDefault();

            if (registry == null)
            {
                SrLogger.LogError("Could not find WeatherRegistry in scene", SrLogTarget.Both);
                return;
            }

            CreateWeatherPatternLookup(registry);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error in EnsureLookupInitialized: {ex}", SrLogTarget.Both);
        }
    }

    public static WeatherPatternDefinition GetPatternForZoneAndState(
        ZoneDefinition zone,
        string stateName)
    {
        EnsureLookupInitialized();

        if (!lookupInitialized)
        {
            SrLogger.LogWarning("Weather pattern lookup not initialized", SrLogTarget.Both);
            return null!;
        }

        if (zone == null || string.IsNullOrEmpty(stateName))
        {
            SrLogger.LogPacketSize($"Invalid zone or state name: zone={zone?.name}, state={stateName}", SrLogTarget.Both);
            return null!;
        }

        if (weatherPatternsByZone.TryGetValue(zone, out var zoneMap))
        {
            if (zoneMap.TryGetValue(stateName, out var pattern))
            {
                return pattern;
            }
        }

        if (weatherPatternsFromStateNames.TryGetValue(stateName, out var fallbackPattern))
        {
            SrLogger.LogWarning(
                $"Using fallback pattern for {zone.name} / {stateName}: {fallbackPattern.name}",
                SrLogTarget.Both
            );
            return fallbackPattern;
        }

        SrLogger.LogPacketSize(
            $"No pattern found for zone {zone.name} / state {stateName}",
            SrLogTarget.Both
        );

        return null!;
    }

    public static void SendWeatherUpdate()
    {
        if (!Main.Server.IsRunning())
            return;

        try
        {
            var weatherRegistry = Resources
                .FindObjectsOfTypeAll<WeatherRegistry>()
                .FirstOrDefault();

            if (weatherRegistry == null)
            {
                SrLogger.LogError("Could not find WeatherRegistry!", SrLogTarget.Both);
                return;
            }

            if (weatherRegistry._model == null)
            {
                SrLogger.LogError("Could not find WeatherRegistry._model!", SrLogTarget.Both);
                return;
            }

            MelonCoroutines.Start(
                WeatherPacket.CreateFromModel(
                    weatherRegistry._model,
                    PacketType.WeatherUpdate,
                    packet => Main.Server.SendToAll(packet)
                )
            );
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error in SendWeatherUpdate: {ex}", SrLogTarget.Both);
        }
    }
}