using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.FX;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
internal sealed class NetworkPlayerFX : MonoBehaviour
{
    public PlayerFXType FXType;

    [UsedImplicitly]
    public void OnEnable()
    {
        if (HandlingPacket)
            return;

        var packet = new PlayerFXPacket
        {
            FX = FXType,
            Position = transform.position
        };

        Main.SendToAllOrServer(packet);
    }
}