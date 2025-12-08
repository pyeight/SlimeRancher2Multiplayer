using SR2E;

namespace SR2MP;

// We should separate the commands from this file later - if possible
// Done - Az

public class HostCommand : SR2ECommand
{
    private static Server? server;

    public override bool Execute(string[] args)
    {
        server = new Server();
        server.Start(1919);
        return true;
    }

    public override string ID => "host";
    public override string Usage => "host <port>";
}

public class JoinCommand : SR2ECommand
{
    public override bool Execute(string[] args)
    {
        // MultiplayerManager.Instance.Connect(args[0]); <- Tarr's code
        return true;
    }

    public override string ID => "join";
    public override string Usage => "join <code>";
}