using System.Net;
using SR2E;
using SR2E.Utils;

namespace SR2MP;

public sealed class HostCommand : SR2ECommand
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

public sealed class ConnectCommand : SR2ECommand
{
    public override string ID => "connect";
    public override string Usage => "connect <ip/domain[:port]>";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();

        if (args.Length < 1)
            return false;

        var input = args[0];
        string ip;
        int port;
        
        if (input.Contains(':'))
        {
            var split = input.Split(':');
            ip = split[0];

            if (!int.TryParse(split[1], out port))
                return false;
        }
        else
        {
            ip = input;
            if (args.Length < 2 || !int.TryParse(args[1], out port))
                return false;
        }
        
        if (ip.StartsWith("[") && ip.EndsWith("]"))
        {
            ip = ip[1..^1];
        }
        
        try
        {
            var addresses = Dns.GetHostAddresses(ip);
            if (addresses.Length > 0)
            {
                ip = addresses[0].ToString();
            }
            else
            {
                SrLogger.LogWarning("IP address incorrect!", SrLogTarget.Both);
                return false;
            }
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)", SrLogTarget.Both);
            return false;
        }

        Main.Client.Connect(ip, port);
        return true;
    }
}