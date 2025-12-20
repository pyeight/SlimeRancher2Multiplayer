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
        server.Start(int.Parse(args[0]), true);
        return true;
    }
}

public class JoinCommand : SR2ECommand
{
    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();

        if (args.Length < 2)
        {
            return false;
        }

        string ip = args[0];
        int port = int.Parse(args[1]);
        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip.Substring(1, ip.Length - 2);
        }

        Main.Client.Connect(ip, port);
        return true;
    }

    public override string ID => "join";
    public override string Usage => "join <ip> <port>";
}