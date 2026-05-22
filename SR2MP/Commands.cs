/*
using System.Net;
using Starlight;
using Starlight.Utils;
using SR2MP.Components.UI;
using SR2MP.Packets;
using SR2MP.Shared.Utils;

namespace SR2MP;

internal sealed class HostCommand : StarlightCommand
{
    private static Server.SR2MPServer? server;

    public override string ID => "host";
    public override string Usage => "host <port>";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(int.Parse(args[0]), true);
        SrLogger.LogMessage("Host command executed!");
        return true;
    }
}

internal sealed class AutoHostCommand : StarlightCommand
{
    private static Server.SR2MPServer? server;

    public override string ID => "autohost";
    public override string Usage => "autohost";

    public override bool Execute(string[] args)
    {
        MenuEUtil.CloseOpenMenu();
        MultiplayerUI.Instance.StartAutoHost();
        SrLogger.LogMessage("Autohost command executed!");
        return true;
    }
}

internal sealed class ChatCommand : StarlightCommand
{
    public override string ID => "chat";
    public override string Usage => "chat <message>";

    public override bool Execute(string[] args)
    {
        if (args.Length < 1)
            SendError("Not enough arguments");

        var msg = string.Join(" ", args);

        var chatPacket = new ChatMessagePacket
        {
            Username = Main.Username,
            Message = msg
        };

        Main.SendToAllOrServer(chatPacket);

        var messageId = $"{Main.Username}_{msg.GetHashCode()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        MultiplayerUI.Instance.RegisterChatMessage(msg, Main.Username, messageId);

        SrLogger.LogMessage("Chat command executed!", SrLogTarget.Both);
        return true;
    }
}

internal sealed class ConnectCommand : StarlightCommand
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

        SrLogger.LogMessage("Connect command executed!", SrLogTarget.Both);
        Main.Client.Connect(ip, port);
        return true;
    }
}

internal sealed class ResyncAllCommand : StarlightCommand
{
    public override string ID => "resync";
    public override string Usage => "resync";

    public override bool Execute(string[] args)
    {
        if (Main.Client.IsConnected)
            Main.Server.reSyncManager.RequestResync();

        if (Main.Server.IsRunning)
            Main.Server.reSyncManager.SynchronizeAll();

        SrLogger.LogMessage("Resync command executed!", SrLogTarget.Both);
        return true;
    }
}

public sealed class RemoveExceptionsCommand : StarlightCommand
{
    public override string ID => "removeexceptions";
    public override string Usage => "removeexceptions";

    public override bool Execute(string[] args)
    {
        Firewall.RemoveAllExceptions();
        SrLogger.LogMessage("removeexceptions command executed!", SrLogTarget.Both);
        return true;
    }
}
*/