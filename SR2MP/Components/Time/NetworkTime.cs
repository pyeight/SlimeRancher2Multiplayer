using Il2Cpp;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Components.Time;

[RegisterTypeInIl2Cpp(false)]
public class NetworkTime : MonoBehaviour
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

        if (sendTimer >= Timers.TimeSyncTimer)
        {
            sendTimer = 0;

            var packet = new WorldTimePacket()
            {
                Type = (byte)PacketType.WorldTime, Time = timeDirector._worldModel.worldTime
            };
            
            Main.Server.SendToAll(packet);
        }
    }
}