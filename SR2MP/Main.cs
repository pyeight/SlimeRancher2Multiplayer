using SR2E.Expansion;
using SR2MP.Shared.Utils;

namespace SR2MP;

public sealed class Main : SR2EExpansionV3
{
    public static Client.Client Client { get; private set; }
    public static Server.Server Server { get; private set; }
    public override void OnLateInitializeMelon()
    {
        MainThreadDispatcher.Initialize();

        Client = new Client.Client();
        Server = new Server.Server();
    }
}