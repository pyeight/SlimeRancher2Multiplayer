using MelonLoader;
using SR2MP.Packets.Utils;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public class NetworkWorldFX : MonoBehaviour
{
    public WorldFXType fxType;

    private void OnEnable()
    {
        SendPacket();
    }

    void SendPacket()
    {
        if (handlingPacket) return;
        
        var packet = new WorldFXPacket()
        {
            Type = (byte)PacketType.WorldFX,
            FX = fxType,
            Position = transform.position
        };
        
        Main.SendToAllOrServer(packet);
    }
}