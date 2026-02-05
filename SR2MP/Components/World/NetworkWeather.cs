using MelonLoader;
using SR2MP.Server.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.World;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkWeather : MonoBehaviour
{
    private float sendTimer;

    private void Update()
    {
        sendTimer += UnityEngine.Time.deltaTime;

        if (sendTimer < Timers.WeatherTimer)
            return;

        sendTimer = 0;

        WeatherUpdateHelper.EnsureLookupInitialized();
        WeatherUpdateHelper.SendWeatherUpdate();
    }
}