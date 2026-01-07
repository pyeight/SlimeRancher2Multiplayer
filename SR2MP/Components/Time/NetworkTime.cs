using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Time;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkTime : MonoBehaviour
{
    private TimeDirector timeDirector;

    private float sendTimer;

    void Awake()
    {
        timeDirector = GetComponent<TimeDirector>();
    }

    void Update()
    {
        sendTimer += UnityEngine.Time.deltaTime;

        if (sendTimer < Timers.TimeSyncTimer)
            return;

        sendTimer = 0;

        var packet = new WorldTimePacket
        {
            Type = (byte)PacketType.WorldTime,
            Time = timeDirector._worldModel.worldTime
        };

        Main.Server.SendToAll(packet);
    }
}