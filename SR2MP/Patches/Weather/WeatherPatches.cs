using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Weather;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using UnityEngine;
using System.Linq;

namespace SR2MP.Patches.Weather
{
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
    

    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.RunState))]
    public static class WeatherDirectorRunStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogWarning("Blocked client weather RunState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogWarning("Allowed client weather RunState", SrLogTarget.Both);
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
                SrLogger.LogWarning("Blocked weather StopState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogWarning("Allowed weather StopState", SrLogTarget.Both);
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

    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.RunPatternState))]
    public static class WeatherRegistryRunPatternStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogWarning("Blocked weather registry RunPatternState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogWarning("Allowed weather registry RunPatternState", SrLogTarget.Both);
            return true;
        }
    }

    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.StopPatternState))]
    public static class WeatherRegistryStopPatternStatePatch
    {
        public static bool Prefix()
        {
            if (Main.Client.IsConnected && !handlingPacket)
            {
                SrLogger.LogWarning("Blocked weather registry StopPatternState", SrLogTarget.Both);
                return false;
            }

            SrLogger.LogWarning("Allowed weather registry StopPatternState", SrLogTarget.Both);
            return true;
        }
    }
}
public static class WeatherUpdateHelper
{
    private static float timeSinceLastUpdate = 0f;
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
    public static void SendWeatherUpdate()
    {
        if (!Main.Server.IsRunning())
            return;

        var weatherRegistry = Resources.FindObjectsOfTypeAll<WeatherRegistry>().FirstOrDefault();
        if (weatherRegistry == null || weatherRegistry._model == null)
        {
            SrLogger.LogError("WeatherRegistry or model not found!", SrLogTarget.Both);
            return;
        }

        MelonCoroutines.Start(
            WeatherPacket.CreateFromModel(
                weatherRegistry._model,
                PacketType.WeatherUpdate,
                packet => Main.Server.SendToAll(packet)
            )
        );

        SrLogger.LogMessage("Sent weather update", SrLogTarget.Both);
    }
}
