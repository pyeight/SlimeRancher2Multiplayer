using MelonLoader;
using SR2MP.Packets.Utils;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public class NetworkPlayerFX : MonoBehaviour
{
    public PlayerFXType fxType;

    private void OnEnable()
    {
        SendPacket();
    }

    void SendPacket()
    {
        if (handlingPacket) return;
        
        var packet = new PlayerFXPacket()
        {
            Type = (byte)PacketType.PlayerFX,
            FX = fxType,
            Position = transform.position
        };
        
        Main.SendToAllOrServer(packet);
    }
}