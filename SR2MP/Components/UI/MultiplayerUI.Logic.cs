using System.Net;
using MelonLoader;
using SR2E;
using SR2E.Utils;

namespace SR2MP.Components.UI;

public sealed partial class MultiplayerUI
{
    public void Host(ushort port)
    {
        Server.Server server;
        MenuEUtil.CloseOpenMenu();
        server = Main.Server;
        server.Start(port, true);
    }
    public void Connect(string ip, ushort port)
    {
        MenuEUtil.CloseOpenMenu();
        
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
            }
        }
        catch
        {
            SrLogger.LogWarning("IP address could not be resolved! (are you connected to the internet?)", SrLogTarget.Both);
        }

        Main.Client.Connect(ip, port);
    }
}