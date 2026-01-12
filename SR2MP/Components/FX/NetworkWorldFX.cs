using MelonLoader;
using SR2MP.Packets.FX;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkWorldFX : MonoBehaviour
{
    public WorldFXType fxType;

    private void OnEnable()
    {
        SendPacket();
    }

    private void SendPacket()
    {
        if (handlingPacket) return;

        var packet = new WorldFXPacket
        {
            FX = fxType,
            Position = transform.position
        };

        Main.SendToAllOrServer(packet);
    }
}