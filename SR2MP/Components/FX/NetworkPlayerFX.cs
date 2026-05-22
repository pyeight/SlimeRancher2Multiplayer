using JetBrains.Annotations;
using MelonLoader;
using SR2MP.Packets.FX;
using Starlight.Storage;

namespace SR2MP.Components.FX;

[InjectIntoIL]
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