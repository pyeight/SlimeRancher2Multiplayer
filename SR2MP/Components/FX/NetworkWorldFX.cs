using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.FX;

namespace SR2MP.Components.FX;

[RegisterTypeInIl2Cpp(false)]
internal sealed class NetworkWorldFX : MonoBehaviour
{
    public WorldFXType FXType;

    [UsedImplicitly]
    public void OnEnable()
    {
        if (HandlingPacket)
            return;

        var packet = new WorldFXPacket
        {
            FX = FXType,
            Position = transform.position
        };

        Main.SendToAllOrServer(packet);
    }
}