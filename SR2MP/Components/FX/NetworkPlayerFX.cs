using MelonLoader;
using SR2MP.Packets.FX;
using SR2MP.Packets.Utils;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkPlayerFX : MonoBehaviour
{
    public PlayerFXType fxType;

    private void OnEnable()
    {
        SendPacket();
    }

    private void SendPacket()
    {
        if (handlingPacket) return;

        var packet = new PlayerFXPacket
        {
            Type = (byte)PacketType.PlayerFX,
            FX = fxType,
            Position = transform.position
        };

        Main.SendToAllOrServer(packet);
    }
}