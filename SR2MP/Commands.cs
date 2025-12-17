using Microsoft.VisualBasic;
using SR2E;
using SR2E.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP;

// We should separate the commands from this file later - if possible
// Done - Az

public class HostCommand : SR2ECommand
{
    private static Server.Server? server;

    public override string ID => "host";
    public override string Usage => "host <port>";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(args.Length == 1 ? int.Parse(args[0]) : 1919);
        return true;
    }
}

public class JoinCommand : SR2ECommand
{
    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();
        Main.Client.Connect(args[0],int.Parse(args[1]));
        return true;
    }

    public override string ID => "join";
    public override string Usage => "join <code>";
}