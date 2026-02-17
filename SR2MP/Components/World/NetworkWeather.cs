using MelonLoader;
using SR2MP.Server.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.World;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkWeather : MonoBehaviour
{
    private float updateTimer;

    private void Update()
    {
        updateTimer += UnityEngine.Time.deltaTime;

        if (updateTimer < Timers.WeatherTimer)
            return;

        updateTimer = 0;

        WeatherUpdateHelper.EnsureLookupInitialized();
        WeatherUpdateHelper.SendWeatherUpdate();
    }
}