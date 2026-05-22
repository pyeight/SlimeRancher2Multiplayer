using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Utils;
using Starlight.Storage;

namespace SR2MP.Components.Time;

[InjectIntoIL]
internal sealed class NetworkTime : MonoBehaviour
{
    private TimeDirector timeDirector;
    private float sendTimer;

    [UsedImplicitly]
    public void Awake() => timeDirector = GetComponent<TimeDirector>();

    public void Update()
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