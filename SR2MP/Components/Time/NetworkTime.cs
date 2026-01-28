using MelonLoader;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Time;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkTime : MonoBehaviour
{
    private TimeDirector timeDirector;

    private float sendTimer;

    private void Awake()
    {
        timeDirector = GetComponent<TimeDirector>();
    }

    private void Update()
    {
        sendTimer += UnityEngine.Time.deltaTime;

        if (sendTimer < Timers.TimeSyncTimer)
            return;

        sendTimer = 0;

        var packet = new WorldTimePacket
        {
            Type = PacketType.WorldTime,
            Time = timeDirector._worldModel.worldTime
        };

        Main.Server.SendToAll(packet);
    }
}