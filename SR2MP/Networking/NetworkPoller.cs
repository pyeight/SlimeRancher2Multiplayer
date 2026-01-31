using MelonLoader;

namespace SR2MP.Networking;

[RegisterTypeInIl2Cpp(false)]
public sealed class NetworkPoller : MonoBehaviour
{
    public void Update()
    {
        Main.Server?.PollEvents();
        Main.Client?.PollEvents();
    }
}
