using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using System.Collections;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Server.Managers;

public static class WeatherUpdateHelper
{
    private static bool lookupInitialized;
    private static bool isInitializing;
    private static readonly Dictionary<string, WeatherPatternDefinition> WeatherPatternsFromStateNames = new();
    private static readonly Dictionary<ZoneDefinition, Dictionary<string, WeatherPatternDefinition>> WeatherPatternsByZone = new();

    public static IEnumerator CreateWeatherPatternLookup(WeatherRegistry registry)
    {
        isInitializing = true;
        
        yield return new WaitFrames(3);
        
        if (registry == null)
        {
            SrLogger.LogError("WeatherRegistry is null in CreateWeatherPatternLookup", SrLogTarget.Both);
            isInitializing = false;
            yield break;
        }

        try
        {
            WeatherPatternsFromStateNames.Clear();
            WeatherPatternsByZone.Clear();

            if (registry.ZoneConfigList == null)
            {
                SrLogger.LogError("WeatherRegistry.ZoneConfigList is null", SrLogTarget.Both);
                isInitializing = false;
                yield break;
            }

            foreach (var config in registry.ZoneConfigList)
            {
                if (!config || !config.Zone || config.Patterns == null)
                {
                    SrLogger.LogPacketSize("Skipping null weather config or patterns", SrLogTarget.Both);
                    continue;
                }

                var zonePatternMap = new Dictionary<string, WeatherPatternDefinition>();

                foreach (var pattern in config.Patterns)
                {
                    if (!pattern || pattern._stateList == null)
                    {
                        SrLogger.LogPacketSize($"Skipping null pattern or state list in zone {config.Zone.name}", SrLogTarget.Both);
                        continue;
                    }

                    foreach (var state in pattern._stateList)
                    {
                        if (!state || string.IsNullOrEmpty(state.name))
                        {
                            SrLogger.LogPacketSize($"Skipping null state or state name in pattern {pattern.name}", SrLogTarget.Both);
                            continue;
                        }

                        zonePatternMap[state.name] = pattern;
                        WeatherPatternsFromStateNames.TryAdd(state.name, pattern);
                    }
                }

                WeatherPatternsByZone[config.Zone] = zonePatternMap;
            }

            lookupInitialized = true;
            SrLogger.LogPacketSize($"Weather pattern lookup initialized with {WeatherPatternsFromStateNames.Count} states", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error in CreateWeatherPatternLookup: {ex}", SrLogTarget.Both);
            lookupInitialized = false;
        }
        finally
        {
            isInitializing = false;
        }
    }

    public static void EnsureLookupInitialized()
    {
        if (lookupInitialized || isInitializing)
            return;

        try
        {
            var registry = SceneContext.Instance.WeatherRegistry;

            if (!registry)
            {
                SrLogger.LogError("Could not find WeatherRegistry in scene", SrLogTarget.Both);
                return;
            }

            MelonCoroutines.Start(CreateWeatherPatternLookup(registry));
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"Error in EnsureLookupInitialized: {ex}", SrLogTarget.Both);
        }
    }

    public static WeatherPatternDefinition GetPatternForZoneAndState(
        ZoneDefinition? zone,
        string stateName)
    {
        EnsureLookupInitialized();

        if (!lookupInitialized)
        {
            SrLogger.LogWarning("Weather pattern lookup not initialized", SrLogTarget.Both);
            return null!;
        }

        if (zone == null)
        {
            SrLogger.LogPacketSize($"Invalid zone: state={stateName}", SrLogTarget.Both);
            return null!;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            SrLogger.LogPacketSize($"Invalid state name: zone={zone.name}, state={stateName}", SrLogTarget.Both);
            return null!;
        }

        if (WeatherPatternsByZone.TryGetValue(zone, out var zoneMap))
        {
            if (zoneMap.TryGetValue(stateName, out var pattern))
            {
                return pattern;
            }
        }

        if (WeatherPatternsFromStateNames.TryGetValue(stateName, out var fallbackPattern))
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
            var weatherRegistry = SceneContext.Instance.WeatherRegistry;

            if (!weatherRegistry)
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